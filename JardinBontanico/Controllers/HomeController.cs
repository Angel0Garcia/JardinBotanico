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

        // Acci�n para la vista principal donde se sube la imagen
        public IActionResult SubirImagen()
        {
            return View(); // Devuelve la vista SubirImagen (index)
        }

        [HttpPost]
        public async Task<IActionResult> SubirImagen(IFormFile florImagen)
        {
            if (florImagen != null && florImagen.Length > 0)
            {
                // Crear la ruta donde se guardar� la imagen
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                var filePath = Path.Combine(uploadsFolder, florImagen.FileName);

                // Crear el directorio si no existe
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Guardar la imagen en el servidor
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await florImagen.CopyToAsync(stream);
                }

                // Llamada a la API para analizar la imagen
                var resultadoAnalisis = await AnalizarImagenConAPI(filePath);

                // Pasamos el resultado a la vista de resultados
                ViewData["Resultado"] = resultadoAnalisis;
                ViewData["Imagen"] = florImagen.FileName;

                return View("Resultado"); // Redirige a la vista Resultado
            }

            // Si no se ha subido una imagen, redirigimos al formulario nuevamente
            ViewData["Error"] = "No se ha subido una imagen v�lida.";
            return View(); // Devuelve la vista SubirImagen con el mensaje de error
        }

        // M�todo para llamar a la API y analizar la imagen
        private async Task<string> AnalizarImagenConAPI(string filePath)
        {
            using (var client = new HttpClient())
            {
                // Configurar el encabezado de autenticaci�n
                client.DefaultRequestHeaders.Add("Prediction-Key", _apiKey);

                // Abrir la imagen y convertirla en un archivo binario
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Enviar la solicitud POST a la API
                var response = await client.PostAsync(_apiUrl, fileContent);

                // Verificar si la respuesta es exitosa
                if (response.IsSuccessStatusCode)
                {
                    // Leer la respuesta JSON
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // Opcional: Aqu� puedes parsear el JSON para extraer los datos espec�ficos que necesitas
                    dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                    var predictions = result.predictions;

                    // Mostrar las predicciones de la API
                    var resultado = new StringBuilder();
                    foreach (var prediction in predictions)
                    {
                        resultado.AppendLine($"Etiqueta: {prediction.tagName}, Confianza: {prediction.probability}");
                    }

                    return resultado.ToString();
                }
                else
                {
                    return "Error al analizar la imagen. C�digo de estado: " + response.StatusCode;
                }
            }
        }

        // Acci�n para mostrar los resultados del an�lisis
        public IActionResult Resultado()
        {
            return View(); // Devuelve la vista Resultado con los datos en ViewData
        }
    }
}