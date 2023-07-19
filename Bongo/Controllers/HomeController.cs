using Bongo.Data;
using Bongo.Infrastructure;
using Bongo.Models;
using Bongo.Models.ViewModels;
using Bongo.Services;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Bongo.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IRepositoryWrapper _repo;
        private readonly IMailService _mailSender;

        public HomeController(IRepositoryWrapper repo, IMailService mailSender)
        {
            _repo = repo;
            _mailSender = mailSender;
        }

        [HttpGet]
        public IActionResult Index()
        {

            if (Request.Cookies["isVenueHidden"] == null)
            {
                SetCookie("isVenueHidden", "false");
            }

            bool isForFirstSemester = Request.Cookies["isForFirstSemester"] == "true";


            #region For Control
            List<List<Session>> ClashesList;
            List<Lecture> GroupedList;
            #endregion

            Timetable table = _repo.Timetable.GetUserTimetable(User.Identity.Name);
            if (table == null)
            {
                return RedirectToAction("TimeTableFileUpload", "Session");
            }

            GetTimeTable timetableGetter = new GetTimeTable(table.TimetableText, isForFirstSemester);
            Session[,] data = timetableGetter.Get(out ClashesList, out GroupedList);

            ViewBag.isForFirstSemester = isForFirstSemester;
            TempData["isForFirstSemester"] = isForFirstSemester;
            return View(new IndexViewModel
            {
                Sessions = data is not null ? data : new Session[5, 16],
            });
        }
        public IActionResult GeneratePdf()
        {
            CookieOptions cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(12) };

            Response.Cookies.Append("isVenueHidden", "false", cookieOptions);

            #region For Control
            List<List<Session>> ClashesList;
            List<Lecture> GroupedList;
            #endregion


            Timetable table_ = _repo.Timetable.GetUserTimetable(User.Identity.Name);

            if (table_ == null)
                RedirectToAction("TimetableFileUpload", "Session");

            GetTimeTable timetableGetter = new GetTimeTable(table_.TimetableText, Request.Cookies["isForFirstSemester"] == "true");
            Session[,] data = timetableGetter.Get(out ClashesList, out GroupedList);



            Session[,] Model = data;
            Document document = new Document();
            MemoryStream memoryStream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
            document.Open();


            // writer.PageEvent = new PdfBackgroundEvent(BaseColor.LIGHT_GRAY);


            // Create a table
            PdfPTable table = new PdfPTable(6); // Adjust the number of columns as per your table

            // Set table properties
            table.WidthPercentage = 100;
            table.SpacingBefore = 10f;
            table.SpacingAfter = 10f;

            // Set table header cell style
            PdfPCell headerCell = new PdfPCell();
            headerCell.BackgroundColor = BaseColor.LIGHT_GRAY;
            headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            headerCell.Padding = 5;
            headerCell.BorderWidth = 0;
            headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            headerCell.VerticalAlignment = Element.ALIGN_CENTER;
            // Add table headers
            table.AddCell(new PdfPCell(new Phrase("Time", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLUE))) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_CENTER, PaddingBottom = 5 });
            table.AddCell(new PdfPCell(new Phrase("Monday", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLUE))) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_CENTER, PaddingBottom = 5 });
            table.AddCell(new PdfPCell(new Phrase("Tuesday", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLUE))) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_CENTER, PaddingBottom = 5 });
            table.AddCell(new PdfPCell(new Phrase("Wednesday", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLUE))) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_CENTER, PaddingBottom = 5 });
            table.AddCell(new PdfPCell(new Phrase("Thursday", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLUE))) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_CENTER, PaddingBottom = 5 });
            table.AddCell(new PdfPCell(new Phrase("Friday", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLUE))) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_CENTER, PaddingBottom = 5 });

            // Set table cell style
            PdfPCell cell = new PdfPCell();
            cell.Padding = 0;
            cell.BorderWidth = 0;

            //Handle Rowspan 
            List<List<int>> lstCells = new List<List<int>>();
            IndexViewModel model = new IndexViewModel { Sessions = Model };
            for (int i = 0; i < model.latestPeriod; i++)
            {
                lstCells.Add(new List<int>());
            }

            // Add table rows
            for (int i = 0; i < model.latestPeriod; i++)
            {
                table.AddCell(new PdfPCell(new Phrase(GetTime(i), new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLUE))) { BackgroundColor = BaseColor.LIGHT_GRAY, HorizontalAlignment = Element.ALIGN_CENTER, VerticalAlignment = Element.ALIGN_MIDDLE });
                for (int j = 0; j < 5; j++)
                {
                    // Get the session and set the cell content
                    Session session = Model[j, i];
                    PdfPCell sessionCell = new PdfPCell();
                    if (session != null)
                    {
                        int row = Extensions.GetInterval(session.sessionInPDFValue);

                        if (row > 1)
                        {
                            for (int k = 0; k < row - 1; k++)
                            {
                                lstCells[i + k].Add(j);
                            }
                        }
                        if (i > 0 && lstCells[i - 1].Contains(j))
                        {

                            continue;
                        }

                        sessionCell.BackgroundColor = GetSessionColor(session);
                        BaseColor FontColor = sessionCell.BackgroundColor.RGB == BaseColor.WHITE.RGB ? BaseColor.BLACK : BaseColor.WHITE;
                        sessionCell.AddElement(new Paragraph(session.ModuleCode, new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, FontColor)));
                        sessionCell.AddElement(new Paragraph(session.Venue, new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL, FontColor)));
                        sessionCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        sessionCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        sessionCell.Rowspan = row;
                        sessionCell.FixedHeight = 50f;
                        sessionCell.BorderWidth = 1f;
                        sessionCell.Padding = 10f;

                        // Add the cell to the table
                        table.AddCell(sessionCell);
                    }
                    else
                    {
                        if (i > 0 && lstCells[i - 1].Contains(j))
                        {
                            continue;
                        }

                        //sessionCell.AddElement(new Paragraph(session.ModuleCode, new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.WHITE)));
                        sessionCell.FixedHeight = 50f;
                        // Add an empty cell
                        table.AddCell(new PdfPCell(new Phrase("Bongoxrxrexerxexexerxre", new Font(Font.FontFamily.HELVETICA, 16, Font.NORMAL, BaseColor.WHITE))) { BackgroundColor = BaseColor.WHITE, HorizontalAlignment = Element.ALIGN_CENTER });
                        // table.AddCell(new PdfPCell(new Phrase(GetTime(i), new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, BaseColor.WHITE))) { BackgroundColor = BaseColor.WHITE });
                    }
                }
            }

            // Add the table to the document
            document.Add(table);

            document.Close();

            // Set the response headers
            Response.Headers.Add("Content-Disposition", "inline;filename=table.pdf");
            Response.Headers.Add("Content-Type", "application/pdf");

            // Return the PDF file
            return File(memoryStream.ToArray(), "application/pdf");
        }

        private string GetTime(int index)
        {
            int hour = index + 7;
            return hour.ToString("D2") + ":00";
        }

        private BaseColor GetSessionColor(Session session)
        {
            var moduleColor = _repo.ModuleColor.GetModuleColorWithColorDetails(User.Identity.Name, session.ModuleCode);


            var color = Aspose.Svg.Drawing.Color.FromString(moduleColor.Color.ColorValue.ToString());
            var co = color.ToRgbaString();
            int r = (int)Math.Round((double)color.Red * 255);
            int g = (int)Math.Round((double)color.Green * 255);
            int b = (int)Math.Round((double)color.Blue * 255);
            return new BaseColor(r, g, b);

        }

        private static BaseColor HexToBaseColor(string hexColor)
        {
            int red = int.Parse(hexColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            int green = int.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int blue = int.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);



            return new BaseColor(red, green, blue);
        }




        [HttpPost]
        public IActionResult UpdateCookie(string key, string value)
        {
            Response.Cookies.Delete(key);
            Response.Cookies.Append(key, value, new CookieOptions() { Expires = DateTime.Now.AddDays(90) });
            return RedirectToAction("Index", "Session", new { isForFirstSemester = Request.Cookies["isForFirstSemester"] });
        }

        private void SetCookie(string key, string value)
        {
            CookieOptions cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(90) };

            Response.Cookies.Append(key, value == null ? "" : value, cookieOptions);
        }
    }
}