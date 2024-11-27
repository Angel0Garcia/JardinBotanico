using JardinBontanico.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace JardinBontanico.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _apiUrl = "https://southcentralus.api.cognitive.microsoft.com/customvision/v3.0/Prediction/5a116914-a14c-4c35-a434-e195edf3c010/classify/iterations/FlowerRecognizer/image";
        private readonly string _apiKey = "bfd4e77a604745068f46196650ec681c"; // Clave API

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Acción para la vista principal donde se sube la imagen
        public IActionResult SubirImagen()
        {
            return View(); // Devuelve la vista SubirImagen (index)
        }

        [HttpPost]
        public async Task<IActionResult> SubirImagen(IFormFile florImagen)
        {
            if (florImagen != null && florImagen.Length > 0)
            {
                // Crear la ruta donde se guardará la imagen
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                var filePath = Path.Combine(uploadsFolder, florImagen.FileName);

                // Crear el directorio si no existe
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await florImagen.CopyToAsync(stream);
                }

                var resultadoAnalisis = await AnalizarImagenConAPI(filePath);

                ViewData["Resultado"] = resultadoAnalisis;
                ViewData["Imagen"] = florImagen.FileName;

                return View("Resultado"); 
            }

            ViewData["Error"] = "No se ha subido una imagen válida.";
            return View(); 
        }

        private async Task<string> AnalizarImagenConAPI(string filePath)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Prediction-Key", _apiKey);

                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var response = await client.PostAsync(_apiUrl, fileContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                    var predictions = result.predictions;

                    var resultado = new StringBuilder();
                    foreach (var prediction in predictions)
                    {
                        resultado.AppendLine($"Tipo: {prediction.tagName}, Probabilidad: {(int)prediction.probability*100}%");
                    }

                    return resultado.ToString();
                }
                else
                {
                    return "Error al analizar la imagen. Código de estado: " + response.StatusCode;
                }
            }
        }

        public IActionResult Resultado()
        {
            return View(); 
        }
    }
}