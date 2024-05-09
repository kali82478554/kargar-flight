using CsvHelper;
using Domain.Context;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.Modules.Implement;
using Repository.Modules.Interface;
using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;

namespace schedule_flight
{
    internal class Program
    {
        static ServiceCollection InjectServiceCollection(ServiceCollection serviceDescriptors)
        {
            var connection = @"Server=DESKTOP-JMMV2AR;Database=Schedule-flight;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            serviceDescriptors.AddDbContext<DataBaseContext>(options => options.UseSqlServer(connection));
            serviceDescriptors.AddScoped(typeof(IRepository<flights>), typeof(Repository<flights>));
            serviceDescriptors.AddScoped(typeof(IRepository<routes>), typeof(Repository<routes>));
            serviceDescriptors.AddScoped(typeof(IRepository<subscriptions>), typeof(Repository<subscriptions>));
            Console.WriteLine("InjectServiceCollection... Done");
            return serviceDescriptors;
        }

        static void CreateDataBase(ServiceProvider serviceProvider)
        {
            using (var context = serviceProvider.GetService<DataBaseContext>())
            {
                context.Database.EnsureCreated();
            }
            Console.WriteLine("CreateDataBase... Done");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var serviceCollection = new ServiceCollection();
            var injectSC = InjectServiceCollection(serviceCollection);
            var serviceProvider = injectSC.BuildServiceProvider();

            // This will create the database if it does not exist.
            CreateDataBase(serviceProvider);

            // Check if three command-line parameters are provided
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: dotnet run <start_date> <end_date> <agency_id>");
                return;
            }

            // Parse command-line parameters
            if (!DateTime.TryParseExact(args[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate))
            {
                Console.WriteLine("Invalid start date format. Please use yyyy-MM-dd format.");
                return;
            }

            if (!DateTime.TryParseExact(args[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDate))
            {
                Console.WriteLine("Invalid end date format. Please use yyyy-MM-dd format.");
                return;
            }

            if (!int.TryParse(args[2], out int agencyId))
            {
                Console.WriteLine("Invalid agency ID. Please enter a valid integer.");
                return;
            }

            // seed data
            SeedRoute(serviceProvider);
            SeedFlights(serviceProvider);
            SeedSubScriptions(serviceProvider);
            // Measure the time taken to execute the change detection algorithm
            Stopwatch stopwatch = Stopwatch.StartNew();
            // scope flights 
            flights flight = ScopeFlights(serviceProvider, Convert.ToInt32(args[2]));
            // scope routes 
            routes route = ScopeRoute(serviceProvider, flight.route_id);
            // scope subscriptions
            List<subscriptions> subscriptions = ScopeSubScriptions(serviceProvider);
            SaveResultCSV(flight, route, subscriptions, args);
            stopwatch.Stop();
            // Display execution metrics
            Console.WriteLine($"Time taken to execute the change detection algorithm: {stopwatch.ElapsedMilliseconds} milliseconds");
        }

        static routes ScopeRoute(ServiceProvider serviceProvider, int routeId)
        {
            try
            {
                routes route = new routes();
                using (var scope = serviceProvider.CreateScope())
                {
                    // inject database and reposigory
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<routes>(dbContext);
                    route = repository.GetById(routeId);
                    if (route == null)
                    {
                        Console.WriteLine("data notfound ScopeRoute... ");
                    }
                }
                return route;
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in ScopeRoute... : " + ex.Message);
                throw;
            }
        }

        static void SeedRoute(ServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("SeedRoute start...");
                // Path to the CSV file
                //string folderPath = Directory.GetCurrentDirectory();
                //string filePath = Path.Combine(folderPath + "//Files", "routes.csv");
                string filePath = Path.Combine("C:\\Users\\kali\\Downloads\\Compressed\\backend-files\\routes.csv");
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<routes>(dbContext);
                    var count = repository.GetCount();
                    if (count < 1)
                    {
                        using (var reader = new StreamReader(filePath))
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            List<routes> records = csv.GetRecords<routes>().ToList();
                            foreach (var record in records)
                            {
                                repository.Add(record);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("SeedRoute exist data...");                        
                    }
                }
                Console.WriteLine("SeedRoute... Done");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in SeedRoute... : " + ex.Message);
                throw;
            }
        }

        static flights ScopeFlights(ServiceProvider serviceProvider, int flight_id)
        {
            try
            {
                flights flights = new flights();
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<flights>(dbContext);
                    flights = repository.GetById(flight_id);
                    if (flights == null)
                    {
                        Console.WriteLine("data notfound ScopeFlights... ");                        
                    }
                }
                return flights;
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in ScopeFlights... : " + ex.Message);
                throw;
            }
        }

        static void SeedFlights(ServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("SeedFlights start...");
                // Path to the CSV file
                string folderPath = Directory.GetCurrentDirectory();
                //string filePath = Path.Combine(folderPath + "//Files", "flights.csv");
                string filePath = Path.Combine("C:\\Users\\kali\\Downloads\\Compressed\\backend-files\\flights.csv");

                flightsCsv flightsCsv = new flightsCsv();
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<flights>(dbContext);
                    var count = repository.GetCount();
                    if (count < 1)
                    {
                        using (var reader = new StreamReader(filePath))
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            List<flightsCsv> records = csv.GetRecords<flightsCsv>().ToList();
                            foreach (var record in records)
                            {
                                flights item = new flights
                                {
                                    airline_id = record.airline_id,
                                    flight_id = record.flight_id,
                                    route_id = record.route_id,
                                    arrival_time = record.arrival_time,
                                    departure_time = record.departure_time,
                                };
                                repository.Add(item);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("SeedFlights exist data...");                        
                    }
                }
                Console.WriteLine("SeedFlights... Done");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in ScopeFlights... : " + ex.Message);
                throw;
            }
        }

        static List<subscriptions> ScopeSubScriptions(ServiceProvider serviceProvider)
        {
            try
            {
                List<subscriptions> subscriptions = new List<subscriptions>();
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<subscriptions>(dbContext);
                    subscriptions = repository.GetAll().ToList();
                }
                return subscriptions;
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in ScopeSubScriptions... : " + ex.Message);
                throw;
            }
        }

        static void SeedSubScriptions(ServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("SeedFlights start...");
                // Path to the CSV file
                string folderPath = Directory.GetCurrentDirectory();
                //string filePath = Path.Combine(folderPath + "//Files", "subscriptions.csv");
                string filePath = Path.Combine("C:\\Users\\kali\\Downloads\\Compressed\\backend-files\\subscriptions.csv");

                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<subscriptions>(dbContext);
                    var count = repository.GetCount();
                    if (count < 1)
                    {
                        using (var reader = new StreamReader(filePath))
                        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                        {
                            List<subscriptions> subscriptions = csv.GetRecords<subscriptions>().ToList();
                            foreach (var record in subscriptions)
                            {
                                repository.Add(record);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("SeedSubScriptions exist data...");                        
                    }
                }
                Console.WriteLine("SeedSubScriptions... Done");
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in ScopeSubScriptions... : " + ex.Message);
                throw;
            }
        }

        static void SaveResultCSV(flights flightsCsv, routes routes, List<subscriptions> lstsubscriptions, string[] args)
        {
            try
            {
                // Get the path to the folder where the console application is located
                string folderPath = Directory.GetCurrentDirectory();
                // Path to the CSV file
                string filePath = Path.Combine(folderPath + "//Files", "results.csv");

                // Write data to CSV file
                if (!File.Exists(filePath))
                {
                    // Write headers if the file does not exist
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine("origin_city_id, destination_city_id, departure_time, arrival_time ,airline_id, status ");
                    }
                }

                var startDate = DateTime.Parse(args[0]);
                var endDate = DateTime.Parse(args[1]);
                // Append data to CSV file
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    // Write data
                    var ori_city_id = routes.origin_city_id;
                    var dest_city_id = routes.destination_city_id;
                    var dep_time = flightsCsv.departure_time;
                    var ar_time = flightsCsv.arrival_time;
                    var airline_id = flightsCsv.airline_id;
                    var status = "New";
                    if (lstsubscriptions.Any(q => q.origin_city_id == routes.origin_city_id && q.destination_city_id == routes.destination_city_id))
                    {
                        if (startDate.Date <= routes.departure_date && endDate.Date >= routes.departure_date)
                            status = "Discontinued";
                    }

                    writer.WriteLine($"{ori_city_id}, {dest_city_id}, {dep_time}, {ar_time}, {airline_id}, {status}");
                    Console.WriteLine($"Data appended to CSV file successfully! File SaveResultCSV");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in SaveResultCSV... : " + ex.Message);
                throw;
            }
        }

    }
}
