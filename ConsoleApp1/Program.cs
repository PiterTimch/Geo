using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task OpenAerialMap(string bbox)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://api.openaerialmap.org/meta?bbox={bbox}&format=tiff";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject data = JObject.Parse(responseBody);

            if (data["results"] != null && data["results"].HasValues)
            {
                var firstResult = data["results"][0];
                Console.WriteLine($"OpenAerialMap: знайдено зображення {firstResult["uuid"]}");
            }
            else
            {
                Console.WriteLine("OpenAerialMap: немає доступних зображень.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenAerialMap помилка: {ex.Message}");
        }
    }

    static async Task SentinelHub(string bbox, string token)
    {
        using HttpClient client = new HttpClient();
        string url = "https://services.sentinel-hub.com/api/v1/process";
        string jsonBody = $@"
        {{
            ""input"": {{
                ""bounds"": {{ ""bbox"": [{bbox}] }},
                ""data"": [{{ ""type"": ""sentinel-2-l2a"" }}]
            }},
            ""output"": {{
                ""width"": 512,
                ""height"": 512,
                ""responses"": [{{ ""identifier"": ""default"", ""format"": {{ ""type"": ""image/jpeg"" }} }}]
            }},
            ""evalscript"": ""//VERSION=3\nfunction setup() {{ return {{ input: [\""B02\"", \""B03\"", \""B04\""], output: {{ bands: 3 }} }}; }}\nfunction evaluatePixel(sample) {{ return [2.5 * sample.B04, 2.5 * sample.B03, 2.5 * sample.B02]; }}""
        }}";

        try
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"SentinelHub: отримано відповідь {responseBody.Substring(0, Math.Min(200, responseBody.Length))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SentinelHub помилка: {ex.Message}");
        }
    }

    static async Task NASA_Worldview(string bbox)
    {
        using HttpClient client = new HttpClient();
        string date = (DateTime.UtcNow.AddDays(-1)).ToString("yyyy-MM-dd") + "T00:00:00Z";
        string url = $"https://wvs.earthdata.nasa.gov/api/v1/snapshot?REQUEST=GetSnapshot&TIME={date}&BBOX={bbox}&CRS=EPSG:4326&LAYERS=MODIS_Terra_CorrectedReflectance_TrueColor&WIDTH=1024&HEIGHT=1024&FORMAT=image/jpeg";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"NASA Worldview: отримано зображення за посиланням {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NASA Worldview помилка: {ex.Message}");
        }
    }

    static async Task GoogleMapsStatic(string bbox, string apiKey)
    {
        using HttpClient client = new HttpClient();
        string url = $"https://maps.googleapis.com/maps/api/staticmap?center={bbox}&zoom=12&size=512x512&maptype=satellite&key={apiKey}";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"Google Maps: отримано зображення за посиланням {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Google Maps помилка: {ex.Message}");
        }
    }

    static async Task Main()
    {
        string bbox = "-74.05,40.65,-73.85,40.85";
        //string sentinelToken = "YOUR_SENTINEL_TOKEN";
        //string googleApiKey = "YOUR_GOOGLE_API_KEY";

        await OpenAerialMap(bbox);
        //await SentinelHub(bbox, sentinelToken);
        await NASA_Worldview(bbox);
        //await GoogleMapsStatic("30.5,50.4", googleApiKey);
    }
}

/*
==========================================================
🌍 API для отримання геоданих (дороги, ґрунти, рельєф) 📡
==========================================================

🛣 **1. Дороги, трафік**
-----------------------------------
🔹 OpenStreetMap (OSM) + Overpass API
   - 📌 Що дає: Дані про дороги, будівлі, річки, кордони
   - 🔗 API: https://overpass-turbo.eu/
   - ✅ Безкоштовний

🔹 Google Roads API
   - 📌 Що дає: Прив’язка GPS до дороги, визначення швидкостей
   - 🔗 Документація: https://developers.google.com/maps/documentation/roads/overview
   - 💰 Платний (200$ безкоштовного ліміту)

🔹 HERE Maps API
   - 📌 Що дає: Дані про трафік, обмеження швидкості, типи доріг
   - 🔗 Документація: https://developer.here.com/documentation
   - 💰 Платний

🌿 **2. Ґрунти, типи покриття**
-----------------------------------
🔹 SoilGrids (FAO/ISRIC)
   - 📌 Що дає: Типи ґрунтів, pH, вологість
   - 🔗 API: https://soilgrids.org/
   - ✅ Безкоштовний

🔹 Copernicus Land Monitoring Service (CLMS)
   - 📌 Що дає: Дані про ліси, водойми, ґрунти
   - 🔗 API: https://land.copernicus.eu/
   - ✅ Безкоштовний (Sentinel Hub)

🔹 NASA SRTM Soil Moisture Data
   - 📌 Що дає: Вологість ґрунту за супутниковими даними
   - 🔗 API: https://earthdata.nasa.gov/
   - ✅ Безкоштовний (реєстрація потрібна)

⛰ **3. Рельєф, висота**
-----------------------------------
🔹 NASA SRTM (Shuttle Radar Topography Mission)
   - 📌 Що дає: Глобальні дані висот, точність ≈30 м
   - 🔗 API: https://lpdaac.usgs.gov/products/srtmgl1v003/
   - ✅ Безкоштовний

🔹 Google Elevation API
   - 📌 Що дає: Висота точки над рівнем моря
   - 🔗 Документація: https://developers.google.com/maps/documentation/elevation/start
   - 💰 Платний (200$ безкоштовного ліміту)

🔹 Mapbox Terrain API
   - 📌 Що дає: Дані рельєфу, схилів, висот
   - 🔗 Документація: https://docs.mapbox.com/api/maps/#mapbox-terrain-rgb
   - 💰 Платний (безкоштовно до 100 000 запитів)

==========================================================
🎯 **API, що повертають TIFF** 📂
----------------------------------------------------------
✅ **Copernicus (Sentinel Hub)** → GeoTIFF, WMS (безкоштовно)  
✅ **NASA SRTM** → GeoTIFF (безкоштовно)  
✅ **SoilGrids** → GeoTIFF (безкоштовно)  
✅ **Google Maps Static API** → TIFF недоступний, тільки PNG/JPEG  
✅ **NASA Worldview Snapshots** → TIFF недоступний  
✅ **Mapbox Terrain API** → TIFF недоступний  
----------------------------------------------------------

*/

