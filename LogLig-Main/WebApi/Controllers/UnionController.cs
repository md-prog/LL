using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AppModel;
using WebApi.Models;
using DataService;
using System.Configuration;

namespace WebApi.Controllers
{
    [RoutePrefix("api/Union")]
    public class UnionController : BaseLogLigApiController
    {
        /// <summary>
        ///  Returns the union details, requested via HTTP GET Method with the specified id.
        /// </summary>
        /// <param name="id">The id of the union to retrieve it's details.</param>
        /// <returns></returns>
        /// // GET: api/union/{union id}
        public HttpResponseMessage Get(int id)
        {
            try
            {
                Union unionEntity = new UnionsRepo().GetById(id);
                if (unionEntity != null)
                {
                    var unionViewModel = new UnionViewModel()
                    {
                        Name = unionEntity.Name,
                        Description = unionEntity.Description,
                        IsHandicapped = unionEntity.IsHadicapEnabled,
                        Logo = unionEntity.Logo,
                        PrimaryImage = unionEntity.PrimaryImage,
                        IndexImage = unionEntity.IndexImage,
                        AssociationIndexInfo = unionEntity.IndexAbout,
                        Address = unionEntity.Address,
                        Phone = unionEntity.ContactPhone,
                        Email = unionEntity.Email
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, unionViewModel);
                }
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Union Not Found");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Internal Server Error occured while executong request");
            }
            
        }

        /// <summary>
        ///  Returns the list of Eilat Tournament PDFs details, requested via HTTP GET Method
        /// </summary>
        /// <param name="id">The id of the union to retrieve it's details.</param>
        /// <returns>["file url 1 or null", "file url 2 or null", "file url 3 or null", "file url 4 or null"]</returns>
        /// // GET: api/union/leagues/{union id}
        [Route("Leagues/{id}")]
        public HttpResponseMessage GetLeagues(int id)
        {
            var routeToPDF = ConfigurationManager.AppSettings["PdfRoute"];
            var urlToPDF = ConfigurationManager.AppSettings["PdfUrl"];

            string[] pdfArr = new string[] { $"{routeToPDF}PDF1.pdf", $"{routeToPDF}PDF2.pdf", $"{routeToPDF}PDF3.pdf", $"{routeToPDF}PDF4.pdf" };
            for (int i = 0; i < pdfArr.Length; i++)
            {
                if (System.IO.File.Exists(pdfArr[i]))
                {
                    pdfArr[i] = $"{urlToPDF}PDF{i + 1}.pdf";
                }
                else
                {
                    pdfArr[i] = null;
                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, pdfArr);
        }


    }
}
