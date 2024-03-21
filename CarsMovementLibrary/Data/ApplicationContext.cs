using CarsMovementLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsMovementLibrary.Data
{
	internal class ApplicationContext : DbContext
	{
		public ApplicationContext()
		{
		}

		public ApplicationContext(DbContextOptions<ApplicationContext> options)
		 : base(options)
		{
		}

		public DbSet<StatesInfo> StateInfo { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=CarSiteDB;Integrated Security=True;Encrypt=False");
			}
		}
	}
}
