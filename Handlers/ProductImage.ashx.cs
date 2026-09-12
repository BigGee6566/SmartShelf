using System;
using System.Linq;
using System.Web;
using SmartShelf.Models;

namespace SmartShelf.Handlers
{
    /// <summary>
    /// Serves a product picture. An uploaded image stored in the database wins; failing that the
    /// request is sent on to the product's ImageUrl; failing that a neutral placeholder is drawn
    /// so no screen is left with a broken image icon.
    /// </summary>
    public class ProductImage : IHttpHandler
    {
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            int productId;
            if (!int.TryParse(context.Request.QueryString["id"], out productId))
            {
                WritePlaceholder(context);
                return;
            }

            try
            {
                using (var db = new SmartShelfContext())
                {
                    var image = db.Products
                        .Where(p => p.ProductId == productId)
                        .Select(p => new { p.ImageData, p.ImageContentType, p.ImageUrl })
                        .FirstOrDefault();

                    if (image == null)
                    {
                        WritePlaceholder(context);
                        return;
                    }

                    if (image.ImageData != null && image.ImageData.Length > 0)
                    {
                        context.Response.ContentType = string.IsNullOrEmpty(image.ImageContentType)
                            ? "image/jpeg"
                            : image.ImageContentType;

                        // Product images change rarely; a short private cache keeps the POS grid
                        // from re-fetching every tile on every postback.
                        context.Response.Cache.SetCacheability(HttpCacheability.Private);
                        context.Response.Cache.SetMaxAge(TimeSpan.FromMinutes(10));

                        context.Response.BinaryWrite(image.ImageData);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(image.ImageUrl))
                    {
                        context.Response.Redirect(image.ImageUrl, false);
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // An image is never important enough to fail a page over.
            }

            WritePlaceholder(context);
        }

        /// <summary>A tiny inline SVG in the site's warm neutral - no file on disk to go missing.</summary>
        private static void WritePlaceholder(HttpContext context)
        {
            context.Response.ContentType = "image/svg+xml";
            context.Response.Cache.SetCacheability(HttpCacheability.Public);
            context.Response.Cache.SetMaxAge(TimeSpan.FromHours(1));
            context.Response.Write(
                "<svg xmlns='http://www.w3.org/2000/svg' width='300' height='300' viewBox='0 0 300 300'>" +
                "<rect width='300' height='300' fill='#F3E9D6'/>" +
                "<circle cx='150' cy='128' r='40' fill='none' stroke='#D6C6A8' stroke-width='6'/>" +
                "<path d='M118 196h64' stroke='#D6C6A8' stroke-width='6' stroke-linecap='round'/>" +
                "<path d='M126 216h48' stroke='#E2D8C3' stroke-width='6' stroke-linecap='round'/>" +
                "</svg>");
        }
    }
}
