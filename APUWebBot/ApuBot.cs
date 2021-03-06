using System;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using APUWebBot.Models;
using OfficeOpenXml;
using System.IO;

//don't forget to change the namespace and the using libraries when implementing this to the app
namespace APUWebBot
{
    /// <summary>
    /// A class for accessing the APU Academic Office homepage and get information
    /// </summary>
    public static class ApuBot
    {

        //the delimiter for dividing the cells
        public const char delimiter = '|';

        //link for english academic calendar
        const string enAcademicCalendarUri = "http://en.apu.ac.jp/academic/top/curriculum_17.html/?c=17";

        const string syllabusSearchUri = "https://portal2.apu.ac.jp/campusp/slbssbdr.do?value%28risyunen%29=2018&value%28semekikn%29=2&value%28kougicd%29=";

        /// <summary>
        /// Get all the links found in the Academic Office menu as a string
        /// </summary>
        /// <returns>The links from page.</returns>
        /// <param name="menu">Menu.</param>
        public static List<string> GetLinksFromMainPage(string menu = "01")
        {

            //Link of the Academic Office homepage, this will be the starting location=
            string uri = enAcademicCalendarUri;

            //XPath syntax for searching the Academic Office page menus
            string xpath = $"//div[contains(@class, 'menu_title curriculum{menu}')]";

            //the url that will be outputted
            string outLink;

            //declare a list that will contain all the uris found in the menj
            var uriList = new List<string>();

            try
            {
                //it loads the html document from the given link
                HtmlWeb web = new HtmlWeb();
                var document = web.Load(uri);

                //this defines the tables in the html document, the ancestor of the element using the defined XPath
                var pageBody = document.DocumentNode.SelectSingleNode(xpath);

                //follow the sibliing of the current node div with the given class
                foreach (var ul in pageBody.SelectNodes("following-sibling::ul"))
                {
                    //go throught the list node and get the href and the inner text
                    foreach (var li in ul.SelectNodes("./li/a"))
                    {
                        //get the value of href in the current node
                        outLink = "http://en.apu.ac.jp" + li.Attributes["href"].Value;

                        //add the uri to the list that is going to get returned
                        uriList.Add(outLink);
                    }
                }
                return uriList;
            }
            catch (Exception)
            {

                //if there is no link, or some other problem comes up, output an
                return null;
            }
        }

        /// <summary>
        /// Scrape all the text from the calendar table. This will only scrape the table for content.
        /// </summary>
        /// <returns>The value from table.</returns>
        /// <param name="uri">URI.</param>
        public static List<string> GetValueFromTable(string uri)
        {
            try
            {
                //the xpath expression for defining the elements
                string xpath = "//table[contains(@class, 'fcktable')]/tbody";

                //load the entire html document from the given uri
                var web = new HtmlWeb();
                var document = web.Load(uri);

                //define the tables in the html document, the ancestor of the element
                var tableBody = document.DocumentNode.SelectSingleNode(xpath);

                var calendarItem = new List<string>();

                //check for 404 error in the website and return error
                if (document.DocumentNode.SelectSingleNode("//title").InnerText == "404ページ")
                    throw new HtmlWebException("The page is showing a 404 error");

                string year = string.Empty;

                //loop through the rows of the table
                foreach (HtmlNode tr in tableBody.SelectNodes("./tr")) //this block is showing errors
                {
                    //declare the row content
                    string rowContent = string.Empty;

                    //declare the column variable
                    int col = 0;

                    //loop every columns within a single table
                    foreach (HtmlNode td in tr.SelectNodes("./td"))
                    {
                        //make the current cell the inner text of the element, and get rid of some parts
                        string currentCell = td.InnerText.Trim()
                            .Replace("  ", "")
                            .Replace("&nbsp;", "Empty")
                            .Replace("&darr;", "New Year's Day")
                            .Replace("&rsquo;", "\'")
                            .Replace("\n(Back-up Examination)", "");

                        //check for the first and second column
                        //because the year column is only available in the first and second row
                        if (col == 0 || col == 1)
                        {
                            //change the date column to Date|Month (only applies for the first row
                            if (currentCell == "Date") { currentCell = "Date" + delimiter + "Month"; }
                            //replace the - to a delimiter
                            currentCell = currentCell.Replace('-', delimiter);
                        }

                        //check if it is the first column
                        if (col == 0)
                        {
                            //if the year is already present, change the year var accordingly
                            if (currentCell.Length == 4) { year = currentCell; }
                            //the year is going to be the content of the first column; date-month
                            else { rowContent = year + delimiter; }
                        }

                        //stack the content with the delimiter till the next row
                        rowContent += currentCell + delimiter;

                        //incrment the column
                        col++;
                    }

                    //cut the last delimiter
                    rowContent = rowContent.Remove(rowContent.Length - 1);

                    calendarItem.Add(rowContent);
                }
                return calendarItem;
            }
            catch (Exception e)
            {
                //failed to get the node from the site
                Debug.WriteLine("Error while getting the list " + e);
                return null;
            }
        }

        /// <summary>
        /// Convert the input event string into a more cleaner format for prcessing
        /// </summary>
        /// <returns>The to date time.</returns>
        /// <param name="inputEvent">Input.</param>
        public static string ChangeDateFormat(string inputEvent)
        {
            Dictionary<string, string> monthToNumber = new Dictionary<string, string>
            {
                {"Jan", "01"},
                {"Feb", "02"},
                {"Mar", "03"},
                {"Apr", "04"},
                {"May", "05"},
                {"Jun", "06"},
                {"Jul", "07"},
                {"Aug", "08"},
                {"Sep", "09"},
                {"Oct", "10"},
                {"Nov", "11"},
                {"Dec", "12"}
            };

            //the input string is formatted like this;
            //[year] | [date] | [month] | [day] | [event] | [Description]
            //this will change the format to [year/month/date]|[day of month]|[holiday]

            //split the input string by the delimiter
            string[] acaEvent = inputEvent.Split(delimiter);

            //convert the string to integer, and back to string for formatting
            int intDay = int.Parse(acaEvent[1]);

            //combine the array into a single array for the date
            string joinedDate = acaEvent[0] + "/" + monthToNumber[acaEvent[2]] + "/" + intDay.ToString("00");

            //check if the Event column is empty, and replace it with the next column
            if (acaEvent[4] == "Empty")
            {
                acaEvent[4] = acaEvent[5];
            }
            //mark national holidays into classes as usual
            else if (acaEvent[5].Contains("Classes as usual"))
            {
                acaEvent[4] = acaEvent[4] + "(" + acaEvent[5] + ")";
            }

            //make the final date time string with adding 
            string dateTime = joinedDate + delimiter + acaEvent[3] + delimiter + acaEvent[4];

            return dateTime;
        }

        /// <summary>
        /// Return the list of all academic events from the academic calendar
        /// </summary>
        /// <returns>The event list</returns>
        public static ObservableCollection<Item> AcademicEventList()
        {
            //this method will output the list of items
            ObservableCollection<Item> items = new ObservableCollection<Item>();

            //menu number 1 represents the Academic Calendar
            //iterate through all the links in the menu
            foreach (string i in GetLinksFromMainPage("01"))
            {
                //get the multiple lists
                foreach (string row in GetValueFromTable(i))
                {
                    //skip to the next iteration if it contains the two characters
                    if (row.Contains("Date") || row.Contains("New Year's Day") && row.Contains("Empty")) { continue; }

                    //feed the finalRow to the ChangeDateFormat method
                    string rowFixed = ChangeDateFormat(row);

                    //convert the string into an array
                    string[] calendarItems = rowFixed.Split(delimiter);

                    //add the item with the given parameters
                    items.Add(new Item
                    {
                        //Id = Guid.NewGuid().ToString(),
                        EventName = calendarItems[2],
                        StartDateTime = calendarItems[0]
                    });
                }
            }
            return items;
        }

        /// <summary>
        /// Downloads the timetables from the academic office website
        /// </summary>
        /// <param name="timetablePageUri">Timetable page URI</param>
        public static List<Stream> GetTimetableAsMemStream(string timetablePageUri)
        {
            string xpath = $"//div[contains(@class, 'entry')]";

            var timetablePaths = new List<string>();

            var timetableStreams = new List<Stream>();

            try
            {
                //it loads the html document from the given link
                HtmlWeb web = new HtmlWeb();
                var document = web.Load(timetablePageUri);
            

                //this defines the xlsx links in the html document, the ancestor of the element using the defined XPath
                var pageBody = document.DocumentNode.SelectSingleNode(xpath);

                //follow the sibling of the current node div with the given class
                foreach (var a in pageBody.SelectNodes("./ul/li/a"))
                {
                    //where the xlsx file is
                    string xlsxDownloadUri = "http://en.apu.ac.jp" + a.Attributes["href"].Value;

                    //create a http request and response
                    var req = WebRequest.Create(xlsxDownloadUri);
                    var response = req.GetResponse();

                    //get the web response into stream
                    Stream stream = response.GetResponseStream();

                    timetableStreams.Add(stream);
                    //timetablePaths.Add(downloadPath + a.InnerText + ".xlsx");
                    //downloadedTimetables.Add(xlsxUri);
                }

                //return the list of byte streams for all the timetables
                return timetableStreams;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Reads the raw text cells in xlsx from the memory stream
        /// </summary>
        /// <returns>The string list of raw xlsx cells</returns>
        /// <param name="xlsxStream">File path.</param>
        public static List<string> ReadRawXlsxFileStream(Stream xlsxStream)
        {
            var rawStringData = new List<string>();

            //convert the given stream into a byte array
            byte[] bin = ReadFully(xlsxStream);

            //create a new Excel package in a memorystream
            using (MemoryStream stream = new MemoryStream(bin))
            using (ExcelPackage excelPackage = new ExcelPackage(stream))
            {
                //loop all worksheets
                foreach (ExcelWorksheet worksheet in excelPackage.Workbook.Worksheets)
                {
                    //get the semester and curriculum of the xlsx file
                    string semesterAndCurriculum = worksheet.Cells[2, 1].Value.ToString();

                    //loop all rows
                    for (int y = worksheet.Dimension.Start.Row; y <= worksheet.Dimension.End.Row; y++)
                    {
                        string row = "";
                        //loop all columns in a row
                        for (int x = worksheet.Dimension.Start.Column; x <= worksheet.Dimension.End.Column; x++)
                        {

                            //check if there is anything in the cell
                            if (worksheet.Cells[y, x].Value != null)
                            {
                                //get rid of the line break if it has it
                                string currentItem = worksheet.Cells[y, x].Value.ToString().Replace("\n", "");

                                //add the current column to the row string
                                row += currentItem + delimiter;
                            }
                            else
                            {
                                //add an empty column if there is no entry in the cell
                                row += "NA" + delimiter;
                            }

                        }
                        //add the semester and curriculum column in the end
                        row += semesterAndCurriculum;
                        //add the string to the list
                        rawStringData.Add(row);
                    }
                }
            }

            return rawStringData;
        }

        /// <summary>
        /// Return the List of Lecture Items from the Academic Office website xlsx file
        /// </summary>
        /// <returns>Lecture Items List</returns>
        public static ObservableCollection<LectureItem> LecturesList()
        {
            var lectures = new ObservableCollection<LectureItem>();

            //loop through the links that has the xlsx file
            foreach (var i in GetTimetableAsMemStream(GetLinksFromMainPage("03")[0]))
            {
                //loop through all the raw strings in the xlsx cell row by row
                foreach (var n in ReadRawXlsxFileStream(i))
                {
                    //split the string with the delimiter, making it to an array
                    var lectureArray = n.Split(delimiter);

                    //only add the items that has a proper subject id
                    if (lectureArray[5] != "NA" && lectureArray[5] != "講義CD/Subject CD")
                    {
                        //split the semester and curriculum string into two
                        string[] semesterOrCurr = lectureArray[15].Split("(");
                        semesterOrCurr[0] = semesterOrCurr[0].Replace("Timetable", "").Trim();
                        semesterOrCurr[1] = semesterOrCurr[1].Replace(")", "");

                        string buildingFloor = lectureArray[4];
                        //change building format
                        if (lectureArray[4] != "T.B.A.")
                        {
                            buildingFloor = lectureArray[4].Replace("-", " building ").Replace("Ⅱ", "II");
                            buildingFloor = buildingFloor.Remove(buildingFloor.Length - 1, 1) + OrderedNumber(lectureArray[4].Remove(0, lectureArray[4].Length - 1)) + " floor";
                        }


                        //change day of week format
                        string dayOfWeek = lectureArray[1].Contains("Session") ? "Session" : lectureArray[1].Replace(".", "").Remove(0, 2);

                        //add the lecture item to the list
                        lectures.Add(new LectureItem
                        {
                            Term = lectureArray[0],
                            DayOfWeek = dayOfWeek,
                            Period = lectureArray[2].Contains("T.B.A.") ? "T.B.A." : OrderedNumber(lectureArray[2].Normalize(System.Text.NormalizationForm.FormKC)) + " Period",
                            Classroom = lectureArray[3].Replace("Ⅱ","II "),
                            BuildingFloor = buildingFloor,
                            SubjectId = lectureArray[5],
                            SubjectNameJP = lectureArray[6],
                            SubjectNameEN = lectureArray[7],
                            InstructorJP = lectureArray[8],
                            InstructorEN = lectureArray[9],
                            Language = lectureArray[10],
                            Grade = OrderedNumber(lectureArray[11].Remove(1, 2)) + " Year",
                            Field = lectureArray[12],
                            APS = lectureArray[13],
                            APM = lectureArray[14],
                            Semester = semesterOrCurr[0].Replace(" Semester", ""),
                            Curriculum = semesterOrCurr[1].Remove(0, 4).Replace(" students", ""),
                            
                            
                        });


                    }
                }
            }
            return lectures;
        }

        /// <summary>
        /// Read the input Stream and convert that into a Byte array
        /// </summary>
        /// <returns>Byte array of Stream</returns>
        /// <param name="input">Input.</param>
        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Converts number strings into ordered form (ex: 1st, 2nd, 3rd...)
        /// </summary>
        /// <returns>Ordered number in string</returns>
        /// <param name="inNumber">In number.</param>
        public static string OrderedNumber(string inNumber)
        {
            string outNumber = "";
            
            switch (inNumber)
            {
                case "1":
                    outNumber = "1st";
                    break;
                case "2":
                    outNumber = "2nd";
                    break;
                case "3":
                    outNumber = "3rd";
                    break;
                default:
                    outNumber = inNumber + "th";
                    break;
            }

            return outNumber;

        }
    }
}
