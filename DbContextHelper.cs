using BarberShopApp.Models;

namespace BarberShopWPF.Helpers
{
	public static class DbContextHelper
	{
        public static string ConnectionString =
    @"Server=.;Database=BarberShopApp;Trusted_Connection=True;TrustServerCertificate=True;";

        public static BarberShopAppContext Create()
		{
			return new BarberShopAppContext();
		}
	}
} //Entity Framework