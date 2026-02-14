using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ECommerceProject.Models;
using ECommerceProject.Services.Interfaces;

namespace ECommerceProject.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(_emailSettings.SMTPServer)
                {
                    Port = _emailSettings.SMTPPort,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {toEmail}. Error: {ex.Message}");
                throw;
            }
        }

        public async Task SendOrderConfirmationEmailAsync(string toEmail, string customerName, int orderId, decimal totalAmount)
        {
            var subject = $"Order Confirmation - Order #{orderId}";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .order-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                        .btn {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>✅ Order Confirmed!</h1>
                        </div>
                        <div class='content'>
                            <p>Hi <strong>{customerName}</strong>,</p>
                            <p>Thank you for your order! We're happy to confirm that we've received your order and it's being processed.</p>
                            
                            <div class='order-details'>
                                <h3>Order Details</h3>
                                <p><strong>Order Number:</strong> #{orderId}</p>
                                <p><strong>Total Amount:</strong> {totalAmount:C}</p>
                                <p><strong>Order Date:</strong> {DateTime.Now:dd MMMM yyyy}</p>
                            </div>

                            <p>We'll send you another email when your order ships.</p>
                            
                            <div style='text-align: center;'>
                                <a href='#' class='btn'>View Order Details</a>
                            </div>

                            <p>If you have any questions, feel free to contact us.</p>
                            <p>Thanks for shopping with us!</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2025 ECommerce Store. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Password Reset Request";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #f59e0b; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .btn {{ display: inline-block; padding: 12px 30px; background: #f59e0b; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .warning {{ background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔐 Password Reset</h1>
                        </div>
                        <div class='content'>
                            <p>You requested to reset your password.</p>
                            <p>Click the button below to reset your password:</p>
                            
                            <div style='text-align: center;'>
                                <a href='{resetLink}' class='btn'>Reset Password</a>
                            </div>

                            <div class='warning'>
                                <strong>⚠️ Security Note:</strong><br>
                                This link will expire in 1 hour. If you didn't request this, please ignore this email.
                            </div>

                            <p>If the button doesn't work, copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; color: #666;'>{resetLink}</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendRegistrationConfirmationEmailAsync(string toEmail, string customerName)
        {
            var subject = "Welcome to ECommerce Store! 🎉";

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .btn {{ display: inline-block; padding: 12px 30px; background: #10b981; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎉 Welcome to ECommerce Store!</h1>
                        </div>
                        <div class='content'>
                            <p>Hi <strong>{customerName}</strong>,</p>
                            <p>Thank you for registering with us! We're excited to have you as part of our community.</p>
                            
                            <p>Your account has been successfully created and you can now:</p>
                            <ul>
                                <li>Browse thousands of products</li>
                                <li>Add items to your cart</li>
                                <li>Track your orders</li>
                                <li>Enjoy exclusive deals</li>
                            </ul>

                            <div style='text-align: center;'>
                                <a href='#' class='btn'>Start Shopping</a>
                            </div>

                            <p>Happy shopping!</p>
                        </div>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}