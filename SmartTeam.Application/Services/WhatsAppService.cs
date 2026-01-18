using SmartTeam.Application.DTOs;
using System.Text;
using System.Web;

namespace SmartTeam.Application.Services;

public class WhatsAppService : IWhatsAppService
{
    public string GenerateWhatsAppUrl(string phoneNumber, string message)
    {
        // Clean phone number (remove spaces, dashes, etc.)
        var cleanPhoneNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        
        // Ensure phone number starts with country code
        if (!cleanPhoneNumber.StartsWith("+"))
        {
            // If no country code, assume Azerbaijan (+994)
            if (cleanPhoneNumber.StartsWith("0"))
            {
                cleanPhoneNumber = "+994" + cleanPhoneNumber.Substring(1);
            }
            else if (!cleanPhoneNumber.StartsWith("994"))
            {
                cleanPhoneNumber = "+994" + cleanPhoneNumber;
            }
            else
            {
                cleanPhoneNumber = "+" + cleanPhoneNumber;
            }
        }

        // URL encode the message
        var encodedMessage = HttpUtility.UrlEncode(message);
        
        // Generate WhatsApp URL
        return $"https://wa.me/{cleanPhoneNumber.Replace("+", "")}?text={encodedMessage}";
    }

    public string FormatOrderMessage(WhatsAppOrderDto orderDto)
    {
        var message = new StringBuilder();
        
        // Header
        message.AppendLine("🛒 *YENİ SİFARİŞ*");
        message.AppendLine();
        
        // Customer Information
        message.AppendLine("👤 *Müştəri məlumatları:*");
        message.AppendLine($"Ad: {orderDto.CustomerName}");
        message.AppendLine($"Telefon: {orderDto.CustomerPhone}");
        message.AppendLine();
        
        // Order Items
        message.AppendLine("📦 *Sifariş detalları:*");
        message.AppendLine();
        
        foreach (var item in orderDto.Items)
        {
            message.AppendLine($"• *{item.ProductName}*");
            if (!string.IsNullOrEmpty(item.ProductDescription))
            {
                // Limit description to first 50 characters
                var shortDescription = item.ProductDescription.Length > 50 
                    ? item.ProductDescription.Substring(0, 50) + "..." 
                    : item.ProductDescription;
                message.AppendLine($"  {shortDescription}");
            }
            message.AppendLine($"  Miqdar: {item.Quantity}");
            message.AppendLine($"  Qiymət: {item.UnitPrice:F2} {orderDto.Currency}");
            message.AppendLine($"  Cəm: {item.TotalPrice:F2} {orderDto.Currency}");
            message.AppendLine();
        }
        
        // Total
        message.AppendLine("💰 *ÜMUMI MƏBLƏĞ:*");
        message.AppendLine($"*{orderDto.TotalAmount:F2} {orderDto.Currency}*");
        message.AppendLine();
        
        // Footer
        message.AppendLine("---");
        message.AppendLine("Bu sifariş SmartTeam sistemi vasitəsilə göndərilmişdir.");
        message.AppendLine($"Tarix: {DateTime.Now:dd.MM.yyyy HH:mm}");
        
        return message.ToString();
    }
}
