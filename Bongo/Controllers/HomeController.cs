using Bongo.Data;
using Bongo.Infrastructure;
using Bongo.Models;
using Bongo.Models.ViewModels;
using Bongo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf;
using Syncfusion.Drawing;

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
        public ActionResult GeneratePdf()
        {
            CookieOptions cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(12) };

            Response.Cookies.Append("isVenueHidden", "false", cookieOptions);

            #region For Control
            List<List<Session>> ClashesList;
            List<Lecture> GroupedList;
            #endregion

            Timetable table_ = _repo.Timetable.GetUserTimetable(User.Identity.Name);

            if (table_ == null)
                return RedirectToAction("TimeTableFileUpload", "Session");

            GetTimeTable timetableGetter = new GetTimeTable(table_.TimetableText, Request.Cookies["isForFirstSemester"] == "true");
            Session[,] data = timetableGetter.Get(out ClashesList, out GroupedList);
            Session[,] Model = data;

            // Create a PDF document
            using (PdfDocument document = new PdfDocument())
            {
                PdfPage page = document.Pages.Add();

                PdfGraphics graphics = page.Graphics;

                RectangleF bounds = new RectangleF(0, 0, document.Pages[0].GetClientSize().Width, 50);
                PdfPageTemplateElement header = new PdfPageTemplateElement(bounds);
                header.Graphics.DrawString("B o n g o", new PdfStandardFont(PdfFontFamily.Helvetica, 22, PdfFontStyle.Bold), PdfBrushes.Blue, bounds);

                document.Template.Top = header;

                PdfLayoutFormat format = new PdfLayoutFormat()
                {
                    Layout = PdfLayoutType.Paginate,
                    Break = PdfLayoutBreakType.FitPage
                };

                PdfGrid pdfGrid = new PdfGrid();
                pdfGrid.Columns.Add(6);
                pdfGrid.Headers.Add(1);


                PdfStringFormat pdfStringFormat = new PdfStringFormat()
                {
                    Alignment = PdfTextAlignment.Center,
                    LineAlignment = PdfVerticalAlignment.Middle
                };

                PdfGridCellStyle pdfGridCellStyle = new PdfGridCellStyle()
                {
                    Font = new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Regular),
                    CellPadding = new PdfPaddings(2f, 2f, 2f, 2f),

                };



                //Headers 
                PdfGridRow pdfGridHeader = pdfGrid.Headers[0];
                pdfGridHeader.Cells[0].Value = "Time";
                pdfGridHeader.Cells[1].Value = "Monday";
                pdfGridHeader.Cells[2].Value = "Tuesday";
                pdfGridHeader.Cells[3].Value = "Wednesday";
                pdfGridHeader.Cells[4].Value = "Thursday";
                pdfGridHeader.Cells[5].Value = "Friday";

                PdfFont headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);

                pdfGridHeader.Style = new PdfGridRowStyle { Font = headerFont };

                //Rows

                pdfGrid.Columns[0].Width = 40f;
                //Handle Rowspan 
                List<List<int>> lstCells = new List<List<int>>();
                for (int i = 0; i < 14; i++)
                {
                    lstCells.Add(new List<int>());
                }

                // Add table rows
                for (int i = 0; i < 14; i++)
                {

                    PdfGridRow pdfGridRow = pdfGrid.Rows.Add();
                    pdfGridRow.Cells[0].Value = GetTime(i);
                    pdfGridRow.Cells[0].Style = new PdfGridCellStyle { Font = headerFont };
                    pdfGridRow.Height = 40f;
                    for (int j = 0; j < 5; j++)
                    {

                        Session session = Model[j, i];
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

                            pdfGridRow.Cells[j + 1].Value = $"{String.Format(session.ModuleCode.ToUpper())}\n{session.Venue}";
                            pdfGridRow.Cells[j + 1].RowSpan = row;
                            pdfGridRow.Cells[j + 1].Style = pdfGridCellStyle;
                            var c = _repo.ModuleColor.GetModuleColorWithColorDetails(User.Identity.Name, session.ModuleCode).Color.ColorValue;
                            Syncfusion.Drawing.Color color = ColorTranslator.FromHtml(c);
                            pdfGridRow.Cells[j + 1].Style = new PdfGridCellStyle { BackgroundBrush = new PdfSolidBrush(new PdfColor(color)), TextBrush = new PdfSolidBrush(new PdfColor(255, 255, 255)) };
                        }
                        else
                        {
                            if (i > 0 && lstCells[i - 1].Contains(j))
                            {
                                continue;
                            }
                            pdfGridRow.Cells[j + 1].Value = "";
                        }
                    }

                }

                for (int x = 1; x < pdfGrid.Columns.Count; x++)
                {
                    pdfGrid.Columns[x - 1].Format = pdfStringFormat;
                    pdfGrid.Columns[x].Width = 90f;
                }
                pdfGrid.Columns[pdfGrid.Columns.Count - 1].Format = pdfStringFormat;



                pdfGrid.Draw(page, new RectangleF(0, 50, page.GetClientSize().Width, page.GetClientSize().Height), format);

                PdfPageTemplateElement footer = new PdfPageTemplateElement(bounds);

                footer.Graphics.DrawString("\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tBongo v1 | \u00A9 " + DateTime.Now.Year,
                    new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold), PdfBrushes.LightGray, bounds);

                document.Template.Bottom = footer;

                MemoryStream memoryStream = new MemoryStream();
                document.Save(memoryStream);
                memoryStream.Position = 0;


                return File(memoryStream.ToArray(), "application/pdf", "timetable.pdf");
            }
        }

        private PdfFont GetFont()
        {
            // Specify the font settings
            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 12f, PdfFontStyle.Regular);

            // Set the text color
            return font;
        }


        private string GetTime(int index)
        {
            int hour = index + 7;
            return hour.ToString("D2") + ":00";
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