using FlightPlanner.Models;
using FlightPlanner.Storage;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightPlanner.Storage
{
    public static class FlightStorage
    {
        private static List<Flight> _flights = new List<Flight>();
        private static int _id = 1;
        private static PageResult pageResult = new PageResult();
        private static object _locker = new object();

        public static Flight GetFlight(int id)
        {
            var flight = _flights.SingleOrDefault(flights => flights.Id == id);

            return flight;
        }

        public static void Cleanup()
        {
            _flights.Clear();
        }

        public static Flight AddFlight(Flight flight)
        {
            lock (_locker)
            {
                flight.Id = _id++;
                _flights.Add(flight);

                return flight;
            }
        }

        public static bool IsFlightInadequate(Flight flight)
        {
            lock (_locker)
            {
                if (flight.From == null || flight.To == null ||
                    string.IsNullOrEmpty(flight.Carrier) ||
                    string.IsNullOrEmpty(flight.DepartureTime) ||
                    string.IsNullOrEmpty(flight.ArrivalTime) ||
                    string.IsNullOrEmpty(flight.From.Country) ||
                    string.IsNullOrEmpty(flight.From.City) ||
                    string.IsNullOrEmpty(flight.From.AirportCode) ||
                    string.IsNullOrEmpty(flight.To.Country) ||
                    string.IsNullOrEmpty(flight.To.City) ||
                    string.IsNullOrEmpty(flight.To.AirportCode))
                {
                    return true;
                }

                if ((flight.From.Country.ToLower().Trim() == flight.To.Country.ToLower().Trim()) && 
                    (flight.From.City.ToLower().Trim() == flight.To.City.ToLower().Trim()) &&
                    (flight.From.AirportCode.ToLower().Trim() == flight.To.AirportCode.ToLower().Trim()))
                {
                    return true;
                }

                var departure = DateTime.Parse(flight.DepartureTime);
                var arrival = DateTime.Parse(flight.ArrivalTime);

                if (DateTime.Compare(arrival, departure) <= 0)
                {
                    return true;
                }

                return false;
            }
        }

        public static bool IsFlightADuplicate(Flight flight)
        {
            lock (_locker)
            {
                if (_flights.Count > 0)
                {
                    var index = _flights.FindIndex(flights => flights.DepartureTime == flight.DepartureTime &&
                    flights.ArrivalTime == flight.ArrivalTime
                    && flights.From.Country == flight.From.Country
                    && flights.From.City == flight.From.City
                    && flights.From.AirportCode == flight.From.AirportCode
                    && flights.To.Country == flight.To.Country
                    && flights.To.City == flight.To.City
                    && flights.To.AirportCode == flight.To.AirportCode
                    && flights.Carrier == flight.Carrier);

                    if (index >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static void DeleteFlight(int id)
        {
            lock (_locker)
            {
                _flights.RemoveAll(flight => flight.Id == id);
            }
        }

        public static List<Airport> FindAirport(string phrase)
        {
            var cleanPhrase = phrase.Trim().ToLower();
            List<Airport> airport = new List<Airport>();

            foreach (var flight in _flights)
            {
                if (flight.From.City.ToLower().Contains(cleanPhrase) ||
                    flight.From.Country.ToLower().Contains(cleanPhrase) ||
                    flight.From.AirportCode.ToLower().Contains(cleanPhrase))
                {
                    airport.Add(flight.From);
                }

                if (flight.To.City.ToLower().Contains(cleanPhrase) ||
                    flight.To.Country.ToLower().Contains(cleanPhrase) ||
                    flight.To.AirportCode.ToLower().Contains(cleanPhrase))
                {
                    airport.Add(flight.To);
                }
            }

            return airport;
        }

        public static PageResult SearchFlight(SearchFlightsRequest request)
        {
            if (request.From == null ||
                request.To == null ||
                request.DepartureDate == null ||
                request.From == request.To)
            {
                return null;
            }

            var flightsFound = _flights.Where(flight => flight.DepartureTime.Contains(request.DepartureDate) &&
            flight.From.AirportCode.Contains(request.From) &&
            flight.To.AirportCode.Contains(request.To)).ToList();
            pageResult.Items = flightsFound;
            pageResult.TotalItems = flightsFound.Count;

            return pageResult;
        }
    }
}
