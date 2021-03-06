using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using APUWebBot.Models;

namespace APUWebBot
{
    class Program
    {

        static void Main(string[] args)
        {
            SreachLectureDemo();
            //ReadTimeTableDemo();
        }

        /// <summary>
        /// This demo will get the values from the academic calendar online, and convert that into a list
        /// </summary>
        static void AcademicCalendarDemo()
        {
            //initiate the CSV file which works like a database
            var csv = new StringBuilder();

            foreach (var item in ApuBot.AcademicEventList())
            {
                //combine all the properties in the object to a single line
                string row = item.StartDateTime + ApuBot.delimiter + item.DayOfWeek + ApuBot.delimiter + item.EventName;

                Console.WriteLine(row);

                csv.AppendLine(row);
            }
            //file path for the output
            string filePath = @"output-calendar.csv";

            //output the csv file
            File.WriteAllText(filePath, csv.ToString());
        }

        /// <summary>
        /// This demo will show how the bot gets the lectures, puts them into a list, and prints them
        /// </summary>
        static void ReadTimeTableDemo()
        {

            try
            {
                //loop through the links that has the xlsx file in the course timetable website
                foreach (var i in ApuBot.LecturesList())
                {

                    Console.WriteLine(i.Term + ApuBot.delimiter 
                    + i.DayOfWeek + ApuBot.delimiter 
                        + i.SubjectNameEN + ApuBot.delimiter 
                    + i.SubjectId + ApuBot.delimiter 
                        + i.Semester + ApuBot.delimiter 
                    + i.Curriculum + ApuBot.delimiter
                        + i.BuildingFloor + ApuBot.delimiter
                    + i.Classroom + ApuBot.delimiter
                        + i.Period + ApuBot.delimiter
                    + i.InstructorEN + ApuBot.delimiter
                        + i.Grade + ApuBot.delimiter);
                }

                Console.WriteLine("There are " + ApuBot.LecturesList().Count + " items in the list");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void SreachLectureDemo()
        {
            Console.WriteLine("Starting up the database...");
            //the list that the engine should look to
            var database = ApuBot.LecturesList();

            while (true)
            {
                Console.WriteLine("Type a search term (type exit to terminate): ");
                string query = Console.ReadLine();
                if (query.ToLower().Contains("exit"))
                {
                    break;
                }

                Console.WriteLine("Searching...");
                var searchTime = System.Diagnostics.Stopwatch.StartNew();

                //the results after the filtering
                var searchResults = new List<LectureItem>();

                //the search algorithm starts here, currently it's just a simple linear filtering
                //loop through the database
                foreach (var i in database)
                {
                    //loop through the tags of the item
                    foreach (var tag in i.SearchTags)
                    {
                        //prevent same results to be in the list
                        if (!searchResults.Contains(i))
                        {
                            //add the item to the list if it contains the word
                            //this will be the main search algorithm
                            if (tag.Contains(query.ToLower()))
                            {
                                searchResults.Add(i);
                            }
                        }
                    }
                }

                //show the search results
                if (searchResults.Count > 0)
                {

                    foreach (var res in searchResults)
                    {
                        //Console.WriteLine(res.SubjectNameEN + " subject ID: " + res.SubjectId);
                        Console.WriteLine(res.Term + ApuBot.delimiter
                        + res.DayOfWeek + ApuBot.delimiter
                            + res.SubjectNameEN + ApuBot.delimiter
                        + res.SubjectId + ApuBot.delimiter
                            + res.Semester + ApuBot.delimiter
                        + res.Curriculum + ApuBot.delimiter
                            + res.BuildingFloor + ApuBot.delimiter
                        + res.Classroom + ApuBot.delimiter
                            + res.Period + ApuBot.delimiter
                        + res.InstructorEN + ApuBot.delimiter
                            + res.Grade + ApuBot.delimiter);
                    }
                    searchTime.Stop();
                    Console.WriteLine("Took " + searchTime.ElapsedMilliseconds.ToString() + " ms");
                    Console.WriteLine("Found " + searchResults.Count + " items");
                    Console.WriteLine("============================");
                }
                else
                {
                    Console.WriteLine("No results found");
                    Console.WriteLine("============================");
                }
            }
        }


    }
}
