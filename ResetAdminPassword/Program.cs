using System;
using System.Threading.Tasks;
using GrapheneSensore.Services;

namespace ResetAdminPassword
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Graphene Sensore - Admin Password Reset");
            Console.WriteLine("========================================");
            Console.WriteLine();
            
            try
            {
                Console.WriteLine("Resetting admin password to default...");
                Console.WriteLine();
                
                bool success = await DatabaseInitializationService.ResetAdminPasswordAsync();
                
                if (success)
                {
                    Console.WriteLine("SUCCESS! Admin password has been reset.");
                    Console.WriteLine();
                    Console.WriteLine("Login Credentials:");
                    Console.WriteLine("  Username: admin");
                    Console.WriteLine("  Password: Admin@123");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("ERROR: Failed to reset admin password.");
                    Console.WriteLine("Admin user not found in database.");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine();
                Console.WriteLine("Please check:");
                Console.WriteLine("1. SQL Server is running");
                Console.WriteLine("2. Connection string is correct");
                Console.WriteLine("3. Database 'Grephene' exists");
                Console.WriteLine();
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
