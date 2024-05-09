using CsvHelper;
using Domain.Context;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Repository.Modules.Implement;
using Repository.Modules.Interface;
using System;
using System.Diagnostics;
using System.Globalization;

namespace schedule_flight
{
    public class FlightsDto
    {
        public int FlightId { get; set; }
        public int OriginCityId { get; set; }
        public int DestinationCityId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int AirlineId { get; set; }
        public string Status { get; set; }
    }

    internal class Program
    {
        static ServiceCollection InjectServiceCollection(ServiceCollection serviceDescriptors)
        {
            var connection = @"Server=DESKTOP-JMMV2AR;Database=Schedule-flight;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            serviceDescriptors.AddDbContext<DataBaseContext>(options => options.UseLazyLoadingProxies().UseSqlServer(connection));
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

        /// <summary>
        /// https://github.com/Tetromize/backend-challenge
        /// dotnet run "2016-11-05" "2016-11-10" "10"
        /// </summary>
        /// <param name="args"></param>
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

            if (!int.TryParse(args[2], out int airlineId))
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
            //flights flight = ScopeFlights(serviceProvider, Convert.ToInt32(args[2]));
            // scope routes 
            //routes route = ScopeRoute(serviceProvider, flight.route_id);
            // scope subscriptions
            //List<subscriptions> subscriptions = ScopeSubScriptions(serviceProvider);
            SaveResultCSV(serviceProvider, startDate, endDate, airlineId);
            //SaveResultCSV(serviceProvider, DateTime.Parse("2016-10-11"), DateTime.Parse("2016-10-5") , 10);
            stopwatch.Stop();
            // Display execution metrics
            Console.WriteLine($"Time taken to execute the change detection algorithm: {stopwatch.ElapsedMilliseconds} milliseconds");
        }

        static void SaveResultCSV(ServiceProvider serviceProvider, DateTime startDate, DateTime endDate, int airlineId)
        {
            try
            {
                // Get the path to the folder where the console application is located
                string folderPath = Directory.GetCurrentDirectory();
                // Path to the CSV file
                string filePath = Path.Combine(folderPath + "//Files", "results.csv");
                //string filePath = Path.Combine("C:\\Users\\kali\\Downloads\\Compressed\\backend-files\\results.csv");

                // Write data to CSV file
                if (!File.Exists(filePath))
                {
                    // Write headers if the file does not exist
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        writer.WriteLine("origin_city_id, destination_city_id, departure_time, arrival_time ,airline_id, status ");
                    }
                }

                // Append data to CSV file
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    // Calculate the date range for new and discontinued flights
                    var newFlightStartDate = startDate.AddDays(-7);
                    var newFlightEndDate = endDate;
                    //var scopeNewFlights = ScopeNewFlights(serviceProvider, newFlightStartDate, newFlightEndDate, airlineId);

                    var discontinuedFlightStartDate = startDate;
                    var discontinuedFlightEndDate = endDate.AddDays(7);
                    //var scopeDiscontinuedFlights = ScopeDiscontinuedFlights(serviceProvider, discontinuedFlightStartDate, discontinuedFlightEndDate, airlineId);
                    var scopeFlights = ScopeFlights(serviceProvider, newFlightStartDate, newFlightEndDate, discontinuedFlightStartDate, discontinuedFlightEndDate, airlineId);

                    // Combine new and discontinued flights                    
                    foreach (var item in scopeFlights)
                    {
                        writer.WriteLine($"{item.OriginCityId}, {item.DestinationCityId}, {item.DepartureTime}, {item.ArrivalTime}, {item.AirlineId}, {item.Status}");
                    }

                    Console.WriteLine($"Data appended to CSV file successfully! File SaveResultCSV");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in SaveResultCSV... : " + ex.Message);
                throw;
            }
        }

        static List<FlightsDto> ScopeFlights
            (ServiceProvider serviceProvider, DateTime newFlightStartDate, DateTime newFlightEndDate, DateTime discontinuedFlightStartDate, DateTime discontinuedFlightEndDate, int airlineId)
        {
            try
            {
                List<FlightsDto> flights = new List<FlightsDto>();
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    //var repository = new Repository<flights>(dbContext);

                    var tolerance = TimeSpan.FromMinutes(30);
                    var lst_flights = dbContext.Flights
                                     .AsNoTracking() // Disable entity tracking
                                     .Where(f => f.airline_id == airlineId && ((f.departure_time >= newFlightStartDate && f.departure_time <= newFlightEndDate) ||
                                                  (f.departure_time >= discontinuedFlightStartDate && f.departure_time <= discontinuedFlightEndDate)))
                                     .ToList();

                    // Get new flights
                    var newFlights = lst_flights
                        .Where(f => f.departure_time >= newFlightStartDate && f.departure_time <= newFlightEndDate &&
                            !lst_flights.Any(df =>
                                df.departure_time >= f.departure_time.Subtract(TimeSpan.FromDays(7)) - tolerance &&
                                df.departure_time <= f.departure_time.Subtract(TimeSpan.FromDays(7)) + tolerance &&
                                df.airline_id == f.airline_id))
                        .Select(f => new FlightsDto
                        {
                            FlightId = f.flight_id,
                            OriginCityId = f.Routes.origin_city_id,
                            DestinationCityId = f.Routes.destination_city_id,
                            DepartureTime = f.departure_time,
                            ArrivalTime = f.arrival_time,
                            AirlineId = f.airline_id,
                            Status = "New"
                        })
                        .ToList();

                    // Get discontinued flights
                    var discontinuedFlights = lst_flights
                        .Where(f => f.departure_time >= discontinuedFlightStartDate && f.departure_time <= discontinuedFlightEndDate &&
                            !lst_flights.Any(nf =>
                                nf.departure_time >= f.departure_time.AddDays(7) - tolerance &&
                                nf.departure_time <= f.departure_time.AddDays(7) + tolerance &&
                                nf.airline_id == f.airline_id))
                        .Select(f => new FlightsDto
                        {
                            FlightId = f.flight_id,
                            OriginCityId = f.Routes.origin_city_id,
                            DestinationCityId = f.Routes.destination_city_id,
                            DepartureTime = f.departure_time,
                            ArrivalTime = f.arrival_time,
                            AirlineId = f.airline_id,
                            Status = "Discontinued"
                        })
                        .ToList();

                    // Combine new and discontinued flights
                    flights = newFlights.Concat(discontinuedFlights).ToList();
                }
                return flights;
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in ScopeFlights... : " + ex.Message);
                throw;
            }
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
                string folderPath = Directory.GetCurrentDirectory();
                string filePath = Path.Combine(folderPath + "//Files", "routes.csv");
                //string filePath = Path.Combine("C:\\Users\\kali\\Downloads\\Compressed\\backend-files\\routes.csv");
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

        static List<FlightsDto> ScopeNewFlights(ServiceProvider serviceProvider, DateTime newFlightStartDate, DateTime newFlightEndDate, int airlineId)
        {
            try
            {
                List<FlightsDto> flights = new List<FlightsDto>();
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<flights>(dbContext);

                    //var queryflights = repository.GetAll();
                    //flights = queryflights
                    //    .Where(f => f.departure_time >= newFlightStartDate && f.departure_time <= newFlightEndDate && f.airline_id == airlineId &&
                    //            !dbContext.Flights.Any(df =>
                    //                df.departure_time >= f.departure_time.Subtract(TimeSpan.FromDays(7)) - TimeSpan.FromMinutes(30) &&
                    //                df.departure_time <= f.departure_time.Subtract(TimeSpan.FromDays(7)) + TimeSpan.FromMinutes(30) &&
                    //                df.airline_id == airlineId))
                    //    .Select(f => new FlightsDto
                    //    {
                    //        FlightId = f.flight_id,
                    //        OriginCityId = f.Routes.origin_city_id,
                    //        DestinationCityId = f.Routes.destination_city_id,
                    //        DepartureTime = f.departure_time,
                    //        ArrivalTime = f.arrival_time,
                    //        AirlineId = f.airline_id,
                    //        Status = "New"
                    //    })
                    //.ToList();
                    var tolerance = TimeSpan.FromMinutes(30);
                    flights = repository
                        .Get(filter: f =>
                            f.departure_time >= newFlightStartDate &&
                            f.departure_time <= newFlightEndDate &&
                            f.airline_id == airlineId &&
                            !repository.Get().Any(df =>
                                df.departure_time >= f.departure_time.Subtract(TimeSpan.FromDays(7)) - tolerance &&
                                df.departure_time <= f.departure_time.Subtract(TimeSpan.FromDays(7)) + tolerance &&
                                df.airline_id == f.airline_id))
                        .Select(f => new FlightsDto
                        {
                            FlightId = f.flight_id,
                            OriginCityId = f.Routes.origin_city_id,
                            DestinationCityId = f.Routes.destination_city_id,
                            DepartureTime = f.departure_time,
                            ArrivalTime = f.arrival_time,
                            AirlineId = f.airline_id,
                            Status = "New"
                        })
                        .ToList();
                }
                return flights;
            }
            catch (Exception ex)
            {
                Console.WriteLine("throw an exception in ScopeNewFlights... : " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// this take to long 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="discontinuedFlightStartDate"></param>
        /// <param name="discontinuedFlightEndDate"></param>
        /// <param name="airlineId"></param>
        /// <returns></returns>
        static List<FlightsDto> ScopeDiscontinuedFlights_Long(ServiceProvider serviceProvider, DateTime discontinuedFlightStartDate, DateTime discontinuedFlightEndDate, int airlineId)
        {
            try
            {
                List<FlightsDto> flights = new List<FlightsDto>();
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
                    var repository = new Repository<flights>(dbContext);

                    var tolerance = TimeSpan.FromMinutes(30);
                    flights = repository.Get(filter: f =>
                            f.departure_time >= discontinuedFlightStartDate &&
                            f.departure_time <= discontinuedFlightEndDate &&
                            f.airline_id == airlineId &&
                            !repository.Get().Any(nf =>
                                nf.departure_time >= f.departure_time.AddDays(7) - tolerance &&
                                nf.departure_time <= f.departure_time.AddDays(7) + tolerance &&
                                nf.airline_id == f.airline_id))
                        .Select(f => new FlightsDto
                        {
                            FlightId = f.flight_id,
                            OriginCityId = f.Routes.origin_city_id,
                            DestinationCityId = f.Routes.destination_city_id,
                            DepartureTime = f.departure_time,
                            ArrivalTime = f.arrival_time,
                            AirlineId = f.airline_id,
                            Status = "Discontinued"
                        })
                        .ToList();
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
                string filePath = Path.Combine(folderPath + "//Files", "flights.csv");
                //string filePath = Path.Combine("C:\\Users\\kali\\Downloads\\Compressed\\backend-files\\flights.csv");

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
                string filePath = Path.Combine(folderPath + "//Files", "subscriptions.csv");
                //string filePath = Path.Combine("C:\\Users\\kali\\Downloads\\Compressed\\backend-files\\subscriptions.csv");

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

    }
}
