Библиотека C# WPF .NET Core 8 для распознавания движения автомобилей на парковке, модель best.onnx - yolov8n
обученная на собственном наборе изображений с камер видеонаблюдения. После инициализации через конструктор необходимо задать поле VideoPath
затем выполнить метод GetCarsMovement, результаты распознавания будут записаны в базу данных CarSiteDB. Определение находится ли машина на 
парковочном месте вычисляется как сумма площадей треугольников в четырехугольном полигоне, если она равна, машина на месте, если нет поиск следующего места.