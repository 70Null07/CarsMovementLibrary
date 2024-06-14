using CarsMovementLibrary.Models;
using Compunet.YoloV8;
using MediaFileProcessor.Models.Common;
using MediaFileProcessor.Models.Enums;
using MediaFileProcessor.Processors;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace CarsMovementLibrary
{
	public class CarsMovement
	{
		private Bitmap ParkingImg { get; set; }

		private readonly Data.ApplicationContext _context;

		private bool CarLeaving { get; set; } = false;
		private bool CarEntering { get; set; } = false;

		private int CountLoses { get; set; }

		private List<Roads> ParkingRoads { get; set; }
		private List<ParkingSpace> ParkingSpaces { get; set; }
		private List<Point> PointMovement = [];

		private int SpaceTaken { get; set; }

		public string VideoPath { get; set; }

		public CarsMovement()
		{
			ParkingRoads =
				[
					// Въезд
					new()
					{
						Borders = [new Point(391, 483), new Point(415, 456), new Point(448, 511), new Point(425, 541)],
					},
					// Тамбур
					new()
					{
						Borders = [new Point(255, 493), new Point(356, 366), new Point(430, 438), new Point(320, 563)],
					},
					// Между первым и вторым рядом
					new()
					{
						Borders = [new Point(413, 423), new Point(568, 214), new Point(587, 241), new Point(430, 438)]
					},
					// Между вторым и третьим рядами
					new()
					{
						Borders = [new Point(356, 366), new Point(549, 128), new Point(574, 147), new Point(382, 390)]
					},
					// Между четвертым и пятым рядами
					new()
					{
						Borders = [new Point(196, 404), new Point(448, 99), new Point(469, 117), new Point(210, 431)]
					},
					// У правой стороны от второго до седьмого ряда
					new()
					{
						Borders = [new Point(386, 42), new Point(413, 14), new Point(550, 128), new Point(521, 163)]
					},
					// Между шестым и  седьмым рядами
					new()
					{
						Borders = [new Point(186, 280), new Point(387, 42), new Point(404, 58), new Point(203, 298)]
					},
					new()
					{
						Borders = [new Point(157, 348), new Point(187, 280), new Point(203, 298), new Point(144, 454)]
					},
					new()
					{
						Borders = [new Point(158, 445), new Point(160, 350), new Point(179, 369), new Point(271, 433)]
					},
					new()
					{
						Borders = [new Point(158, 445), new Point(211, 433), new Point(268, 522), new Point(253, 533)]
					},
				];
            //var json = File.ReadAllText("./parkingplaces.json");
            var json = File.ReadAllText(System.IO.Path.GetFullPath(@"..\..\parkingplaces.json"));

            var jObject = JObject.Parse(json);
            try
            {
                ParkingSpaces = jObject["ParkingSpaces"].ToObject<List<ParkingSpace>>();
            }
			catch (Exception)
            {
				ParkingSpaces = new List<ParkingSpace>();
            }
			if (ParkingSpaces == null)

			ParkingImg = new(0, 0);
			VideoPath = string.Empty;

            _context = new Data.ApplicationContext();
		}

		public void GenerateNewData()
		{
            Random rnd = new Random();
            DateTime startDateTime = new DateTime(2024, 3, 25);
            DateTime endDateTime = new DateTime(2024, 4, 1);
            int parkingSpots = 115;
            Dictionary<string, DateTime> parkingState = new Dictionary<string, DateTime>();

            while (startDateTime < endDateTime)
            {
                int carsToEnter = 0;
                if (startDateTime.Hour >= 18 && startDateTime.Hour <= 20) // вечер
                {
                    carsToEnter = rnd.Next(50, 77); // случайное количество автомобилей въезжает каждый вечер
                }
                else if (startDateTime.Hour >= 7 && startDateTime.Hour <= 8) // утро
                {
                    carsToEnter = rnd.Next(1, 10); // меньшее количество автомобилей въезжает утром
                }
                else // день
                {
                    carsToEnter = rnd.Next(1, 5); // случайное количество автомобилей въезжает в течение дня
                }

                for (int i = 0; i < carsToEnter; i++)
                {
                    if (parkingState.Count < parkingSpots)
                    {
                        string carId = "id" + rnd.Next(1010, 9990);
                        int hoursToStay = startDateTime.Hour >= 18 && startDateTime.Hour <= 20 ? rnd.Next(10, 12) : rnd.Next(1, 7);
                        parkingState[carId] = startDateTime.AddHours(hoursToStay);

                        StatesInfo movement = new();
                        movement.DateTime = startDateTime.AddMinutes(rnd.Next(60)); // добавляем вариацию по минутам
                        movement.Plate = carId;
                        movement.State = "Enter pl " + rnd.Next(1, parkingSpots);
                        _context.StateInfo.Add(movement);
                    }
                }

                List<string> carsToLeave = parkingState.Where(p => startDateTime >= p.Value).Select(p => p.Key).ToList();
                foreach (string car in carsToLeave)
                {
                    StatesInfo movement = new StatesInfo();
                    movement.DateTime = startDateTime.AddMinutes(rnd.Next(60)); // добавляем вариацию по минутам
                    movement.Plate = car;
                    movement.State = "Leave pl " + rnd.Next(1, parkingSpots);
                    _context.StateInfo.Add(movement);

                    parkingState.Remove(car);
                }

                startDateTime = startDateTime.AddMinutes(1); // увеличиваем время на 1 минуту
            }

            _context.SaveChanges();
        }

		public void GetCarsMovement()
		{
			var videoFileProcessor = new VideoFileProcessor();

			CountLoses = 0;

			bool isCurrentCarLeaving = false;
			bool isCurrentCarEntering = false;

			Directory.Delete("C:\\temp\\ImgSplit\\", true);
			Directory.CreateDirectory("C:\\temp\\ImgSplit\\");

			for (int i = 0; i < 79000; i += 2000)
			{
				videoFileProcessor.ExtractFrameFromVideoAsync(TimeSpan.FromMilliseconds(i),
	new MediaFile(VideoPath),
	"C:/temp/ImgSplit/img_split_" + i + ".jpg",
	FileFormatType.JPG);

				while (!File.Exists("C:/temp/ImgSplit/img_split_" + i + ".jpg"))
					Thread.Sleep(50);

				if (File.Exists("C:/temp/ImgSplit/img_split_" + i + ".jpg"))
				{
                    try
                    {
                        // Возникает ошибка!!!
                        ParkingImg = new Bitmap("C:/temp/ImgSplit/img_split_" + i + ".jpg");
                    }
					catch (Exception)
                    {
						continue;
					}
                }

				ResizeImage(640, 640);

				isCurrentCarLeaving = false;
				isCurrentCarEntering = false;

				var p1 = ParkingImg.GetPixel(347, 239).R;
				var p2 = ParkingImg.GetPixel(349, 239).R;

				if (!(p1 > 90) || !(p2 > 90))
				{
					using YoloV8Predictor predictor = YoloV8Predictor.Create("./best.onnx");

					Compunet.YoloV8.Data.DetectionResult result = predictor.Detect("./cropped_movement.jpg");

					List<int> placestaken = [];

					for (int j = 0; j < result.Boxes.Length; j++)
					{
						Point boxCenter = new(result.Boxes[j].Bounds.Left + result.Boxes[j].Bounds.Width / 2,
							result.Boxes[j].Bounds.Top + result.Boxes[j].Bounds.Height / 2);

						foreach (var road in ParkingRoads)
						{
							// Для любых трех точек a(x1;y1), b(x2;y2) и c(x3;y3), не лежащих на одной Прямой,
							// Площадь S треугольника авс находится По формуле:
							// Sabc=1/2 |(x2 – x1)(y3 –y1) – (x3 – x1)(y2 – y1)|.
							double s1 = 0.5 * Math.Abs((road.Borders[0].X - boxCenter.X) * (road.Borders[1].Y - boxCenter.Y)
								- (road.Borders[1].X - boxCenter.X) * (road.Borders[0].Y - boxCenter.Y));
							double s2 = 0.5 * Math.Abs((road.Borders[1].X - boxCenter.X) * (road.Borders[2].Y - boxCenter.Y)
								- (road.Borders[2].X - boxCenter.X) * (road.Borders[1].Y - boxCenter.Y));
							double s3 = 0.5 * Math.Abs((road.Borders[2].X - boxCenter.X) * (road.Borders[3].Y - boxCenter.Y)
								- (road.Borders[3].X - boxCenter.X) * (road.Borders[2].Y - boxCenter.Y));
							double s4 = 0.5 * Math.Abs((road.Borders[3].X - boxCenter.X) * (road.Borders[0].Y - boxCenter.Y)
								- (road.Borders[0].X - boxCenter.X) * (road.Borders[3].Y - boxCenter.Y));
							// Площадь произвольного четырехугольника, вершины которого заданы координатами
							// (х1; у1), (х2; у2), (х3; у3), (х4; у4), можно найти по формуле:
							// S = (|(х1 - х2)(у1 + у2) + (х2 - х3)(у2 + у3) + (х3 - х4)(у3 + у4) + (х4 - х1)(у4 + у1)|) / 2.
							double s_full = 0.5 * Math.Abs((road.Borders[0].X - road.Borders[1].X) * (road.Borders[0].Y + road.Borders[1].Y) +
								(road.Borders[1].X - road.Borders[2].X) * (road.Borders[1].Y + road.Borders[2].Y) +
								(road.Borders[2].X - road.Borders[3].X) * (road.Borders[2].Y + road.Borders[3].Y) +
								(road.Borders[3].X - road.Borders[0].X) * (road.Borders[3].Y + road.Borders[0].Y));

							if (s_full == (s1 + s2 + s3 + s4))
							{
								if (!CarLeaving && !CarEntering && road.Borders[0].X == 391)
								{
									CarEntering = true;
									PointMovement.Add(new Point(boxCenter.X, boxCenter.Y));
								}
								else if (!CarLeaving && !CarEntering && road.Borders[0].X != 391)
								{
									CarLeaving = true;

									double minDist = 1000;

									int spot = 1;

									foreach (var p in ParkingSpaces)
									{
										for (int k = 0; k < 2; k++)
										{
											var distance = Math.Sqrt((p.Vertices[k].X - boxCenter.X) * (p.Vertices[k].X - boxCenter.X) +
											(p.Vertices[k].Y - boxCenter.Y) * (p.Vertices[k].Y - boxCenter.Y));
											if (distance < minDist)
											{
												minDist = distance;
												SpaceTaken = spot;
											}
										}
										spot++;
									}

									PointMovement.Add(new Point(boxCenter.X, boxCenter.Y));
								}
								else if (CarLeaving && road.Borders[0].X == 391)
								{
									PointMovement.Add(new Point(boxCenter.X, boxCenter.Y));

                                    StatesInfo movement = new StatesInfo
                                    {
                                        DateTime = DateTime.Now
                                    };
                                    Random rnd = new Random();
									movement.Plate = "id" + rnd.Next(1010, 9990);
									movement.Points = string.Empty;
									foreach (Point point in PointMovement)
									{
										movement.Points += "{" + point.X + "," + point.Y + "};";
									}

									movement.State = "Leave pl " + SpaceTaken;
									_context.StateInfo.Add(movement);
									_context.SaveChanges();

									CarLeaving = false;
									PointMovement.Clear();
								}
								else
								{
									PointMovement.Add(new Point(boxCenter.X, boxCenter.Y));
									if (CarEntering)
										isCurrentCarEntering = true;
								}
							}
						}
					}

					if (CarEntering && !isCurrentCarEntering && CountLoses < 3)
					{
						CountLoses++;
					}
					else if (CarEntering && CountLoses == 3)
					{
						if (PointMovement.Count > 0)
						{
							Point boxCenter = PointMovement.Last();

                            StatesInfo movement = new()
                            {
                                DateTime = DateTime.Now
                            };
                            Random rnd = new();
							movement.Plate = "id" + rnd.Next(1010, 9990);
							movement.Points = string.Empty;
							foreach (Point point in PointMovement)
							{
								movement.Points += "{" + point.X + "," + point.Y + "};";
							}

							int spot = 1;
							double minDist = 1000;

							foreach (var p in ParkingSpaces)
							{
								for (int k = 0; k < 2; k++)
								{
									var distance = Math.Sqrt((p.Vertices[k].X - boxCenter.X) * (p.Vertices[k].X - boxCenter.X) +
									(p.Vertices[k].Y - boxCenter.Y) * (p.Vertices[k].Y - boxCenter.Y));
									if (distance < minDist)
									{
										minDist = distance;
										SpaceTaken = spot;
									}
								}
								spot++;
							}
							movement.State = "Enters pl " + SpaceTaken;
							_context.StateInfo.Add(movement);
							_context.SaveChanges();
						}
						CountLoses = 0;
						PointMovement.Clear();
						CarEntering = false;
					}
				}
			}
		}

		private void ResizeImage(int width, int height)
		{
			Bitmap cropped = new(952, 612);

			// Create a Graphics object to do the drawing, *with the new bitmap as the target*
			using (Graphics g = Graphics.FromImage(cropped))
			{
				// Draw the desired area of the original into the graphics object
				g.DrawImage(ParkingImg, new Rectangle(0, 0, 952, 612), new Rectangle(0, 618, 952, 612), GraphicsUnit.Pixel);
			}
			ParkingImg = cropped;
			ParkingImg.Save("./resized_movement.jpg");

			Bitmap resizedImage = new(width, height);
			using (Graphics graphics = Graphics.FromImage(resizedImage))
			{
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.Bilinear;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.DrawImage(ParkingImg, 0, 0, width, height);
			}

			ParkingImg = resizedImage;
			ParkingImg.Save("./cropped_movement.jpg");
		}
	}

	public class Roads
	{
		public required Point[] Borders { get; set; }
	}

	// Класс для представления парковочного места
	public class ParkingSpace
	{
		// Поле представления вершин квадрата возможной точки
		public Point[] Vertices { get; set; }
		// Поле представления границ полигона парковочного места
		public Point[] Borders { get; set; }

		public ParkingSpace()
		{
			Vertices = [];
			Borders = [];
		}
	}
}
