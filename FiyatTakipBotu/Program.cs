using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static IConfigurationRoot? Configuration;
    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(@"C:\Users\PC\Desktop\C#\FiyatTakipBotu")
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        Configuration = builder.Build();

        if (Configuration == null)
        {
            Console.WriteLine("Configuration null! Lütfen yapılandırma dosyasını kontrol edin.");
            return;
        }
        else
        {
            await FiyatKontrolEt("https://www.superstep.com.tr/urun/vans-old-skool-unisex-siyah-sneaker/vd3hy28/", 3699);
            await FiyatKontrolEt("https://www.superstep.com.tr/urun/vans-ua-old-skool-erkek-kahverengi-sneaker/vn0005uf9jc1/", 3699);
            await FiyatKontrolEt("https://www.superstep.com.tr/urun/converse-cons-day-one-classic-unisex-sneaker/a15626c/", 2599);
            await FiyatKontrolEt("https://www.fashfed.com/urun/vans-old-skool-unisex-siyah-sneaker-vd3hy28/", 3699);
            await FiyatKontrolEt("https://www.fashfed.com/urun/vans-knu-skool-unisex-siyah-sneaker-vn0009qc6bt1/", 3999);
            await FiyatKontrolEt("https://www.fashfed.com/urun/lacoste-t-clip-erkek-bej-sneaker-2-747sma0067t/", 3749);
            await FiyatKontrolEt("https://www.fashfed.com/urun/vans-lowland-cc-erkek-kahverengi-sneaker-1-vn000bwb9jc1-4/", 3569);

            await FiyatKontrolEt("https://kaptanspor.com.tr/adidas-gri-erkek-gunluk-spor-ayakkabisi-run80s-id1882-P390?srsltid=AfmBOoo-ibI9RW18LpX2a0WrAmPpPpL2FXLBIu8oVZZBUlMe7ZONyTPffdc", 2501);
        }

    }

    static async Task<string> SiteyeIstekdeBulun(string url)
    {
        try
        {
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            }

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception)
        {
            return "Hata";
        }

    }
    static async Task<string> HtmlBilgilerindenFiyatCek(string url)
    {
        HtmlNode fiyatElementi;
        string fiyat = "-1";

        string urlTemp = url;
        urlTemp = urlTemp.Replace("https://", "").TrimStart().Split('/')[0];

        string response = await SiteyeIstekdeBulun(url);

        if (response == null || response == "Hata")
        {
            return fiyat;
        }
        else
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(response);

            switch (urlTemp)
            {
                case "www.fashfed.com":
                    fiyatElementi = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'price')]/span[contains(@class, 'price__new')]");
                    if (fiyatElementi != null)
                    {
                        fiyat = fiyatElementi.InnerText.Trim();
                        fiyat = fiyat.Replace("₺ ", "").Replace(".", "").Replace(",00", "");
                    }
                    break;
                case "www.nike.com":
                    fiyatElementi = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'mb4-sm mb8-lg')]/div/span[contains(@class, 'nds-text mr2-sm')]");
                    if (fiyatElementi != null)
                    {
                        fiyat = fiyatElementi.InnerText.Trim();
                        fiyat = fiyat.Replace("₺", "").Replace(".", "").Split(',')[0];
                    }
                    break;
                case "www.superstep.com.tr":
                    fiyatElementi = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'flex')]/div/span[contains(@class, 'text-lg leading-6 md:leading-7 font-bold h-full inline-block')]");
                    if (fiyatElementi != null)
                    {
                        fiyat = fiyatElementi.InnerText.Trim();
                        fiyat = fiyat.Replace(".", "").Replace(" TL", "");
                    }
                    break;
                case "kaptanspor.com.tr":
                    fiyatElementi = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'tf-product-info-price')]//div[contains(@class, 'price-on-sale')]");
                    if (fiyatElementi != null)
                    {
                        fiyat = fiyatElementi.InnerText.Trim();
                        fiyat = fiyat.Replace(",", "").Split('.')[0];
                    }
                    break;

            }
            return fiyat;
        }
    }
    static void MailGonder(string urunUrl, int fiyat)
    {
        string? fromMail = Configuration["EmailSettings:fromMail"]; // Gönderen e-posta adresi
        string? toMail = Configuration["EmailSettings:toMail"];  // Alıcı e-posta adresi
        string? smtpUser = Configuration["EmailSettings:smtpUser"]; // SMTP kullanıcı adı
        string? smtpPass = Configuration["EmailSettings:smtpPass"];  // Google Uygulama Şifresi

        if (fromMail != null && toMail != null)
        {
            MailMessage mail = new MailMessage(fromMail, toMail)
            {
                Subject = "Fiyat Takip Botu - Fiyat Düştü!",
                Body = $"Merhaba,\n\nAşağıdaki ürünün fiyatı düştü:\n\nÜrün Linki: {urunUrl}\nGüncel Fiyat: {fiyat}₺\n",
                IsBodyHtml = false
            };

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            try
            {
                smtp.Send(mail);
                Console.WriteLine("E-posta başarıyla gönderildi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }
        }
    }
    static async Task FiyatKontrolEt(string url, int istenilenFiyat)
    {
        int intGuncelFiyat = Int32.Parse(await HtmlBilgilerindenFiyatCek(url));

        if (intGuncelFiyat == -1)
        {
            Console.WriteLine($"Ürün: {url} - Fiyat bilgisi bulunamadı!\n");
        }
        else
        {
            if (intGuncelFiyat < istenilenFiyat)
            {
                MailGonder(url, intGuncelFiyat);
            }
            else
            {
                Console.WriteLine("İşlem başarıyla tamamlandı.");
            }
        }
    }
}
