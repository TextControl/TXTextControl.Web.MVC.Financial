using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using tx_merchant_sample.Models;
using TXTextControl;
using TXTextControl.DocumentServer;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tx_merchant_sample.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

		private void FlattenFormFields(ServerTextControl textControl)
		{
			int fieldCount = textControl.FormFields.Count;

			for (int i = 0; i < fieldCount; i++)
			{
				TextFieldCollectionBase.TextFieldEnumerator fieldEnum =
				  textControl.FormFields.GetEnumerator();
				fieldEnum.MoveNext();

				FormField curField = (FormField)fieldEnum.Current;
				textControl.FormFields.Remove(curField, true);
			}
		}

		[HttpPost]
        public string CreatePdf([FromBody] TXTextControl.Web.MVC.DocumentViewer.Models.SignatureData signatureData)
        {
            byte[] bPDF;

			// create temporary ServerTextControl
			using (TXTextControl.ServerTextControl tx = new TXTextControl.ServerTextControl())
			{
				tx.Create();

				// load the document
				tx.Load(Convert.FromBase64String(signatureData.SignedDocument.Document),
				  TXTextControl.BinaryStreamType.InternalUnicodeFormat);

				FlattenFormFields(tx);

				// create a certificate
				X509Certificate2 cert = new X509Certificate2("App_Data/textcontrolself.pfx", "123");

				// assign the certificate to the signature fields
				TXTextControl.SaveSettings saveSettings = new TXTextControl.SaveSettings()
				{
					CreatorApplication = "TX Text Control Sample Application",
					SignatureFields = new DigitalSignature[] {
			            new TXTextControl.DigitalSignature(cert, null, "txsign")
		              }
				};

				// save the document as PDF
				tx.Save(out bPDF, TXTextControl.BinaryStreamType.AdobePDFA, saveSettings);
			}

			// return as Base64 encoded string
			return Convert.ToBase64String(bPDF);
		}

        public IActionResult Index()
        {
            using (TXTextControl.ServerTextControl tx = new TXTextControl.ServerTextControl())
            {
                tx.Create();
                tx.Load("App_Data/gpay_application.tx", TXTextControl.StreamType.InternalUnicodeFormat);
                
                var jsonData = System.IO.File.ReadAllText("App_Data/data.json");

                using (MailMerge mm = new MailMerge())
                {
					mm.TextComponent = tx;
                    mm.FormFieldMergeType = FormFieldMergeType.Preselect;
                    mm.MergeJsonData(jsonData);
				}

                byte[] data;
                tx.Save(out data, TXTextControl.BinaryStreamType.InternalUnicodeFormat);

                ViewBag.Document = Convert.ToBase64String(data);
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
