using SerenityHavenResort.Models;
using SerenityHavenResort.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SerenityHavenResort.Services
{

        public class PaymentService
        {
            private readonly IConfiguration _configuration;
            private readonly ILogger<PaymentService> _logger;
            private readonly HttpClient _httpClient;
            private readonly AppDbContext _context;
            private readonly IBookingRepository _bookingRepository; // Use specific repository
            private readonly IRepository<Payment> _paymentRepository; // Use typed repository

            public PaymentService(
                IConfiguration configuration,
                ILogger<PaymentService> logger,
                HttpClient httpClient,
                AppDbContext context,
                IBookingRepository bookingRepository,
                IRepository<Payment> paymentRepository)
            {
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
                _context = context ?? throw new ArgumentNullException(nameof(context));
                _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
                _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));

                StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
            }

            public async Task<string> CreatePaymentIntentAsync(int amount, string currency = "usd")
            {
                try
                {
                    var options = new PaymentIntentCreateOptions
                    {
                        Amount = amount, // Amount in cents
                        Currency = currency,
                        PaymentMethodTypes = new List<string> { "card" }
                    };

                    var service = new PaymentIntentService();
                    var paymentIntent = await service.CreateAsync(options);
                    _logger.LogInformation("Stripe PaymentIntent created: {PaymentIntentId}", paymentIntent.Id);
                    return paymentIntent.Id; // Return PaymentIntent ID
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Error creating Stripe PaymentIntent");
                    throw new PaymentServiceException("Failed to create Stripe PaymentIntent", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error creating Stripe PaymentIntent");
                    throw new PaymentServiceException("Unexpected error creating Stripe PaymentIntent", ex);
                }
            }

            public async Task<Session> CreateCheckoutSessionAsync(int bookingId, int customerId, string successUrl, string cancelUrl)
            {
                try
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                    if (booking == null)
                    {
                        _logger.LogWarning("Booking {BookingId} not found for checkout session", bookingId);
                        throw new InvalidOperationException("Booking not found.");
                    }

                    var amountInCents = (int)(booking.TotalPrice * 100);
                    var options = new SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = amountInCents,
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Booking #{bookingId} - {booking.Room?.RoomType ?? "Room"}"
                                }
                            },
                            Quantity = 1
                        }
                    },
                        Mode = "payment",
                        SuccessUrl = successUrl,
                        CancelUrl = cancelUrl,
                        Metadata = new Dictionary<string, string>
                    {
                        { "BookingId", bookingId.ToString() },
                        { "CustomerId", customerId.ToString() }
                    }
                    };

                    var service = new SessionService();
                    var session = await service.CreateAsync(options);
                    _logger.LogInformation("Created Stripe checkout session {SessionId} for Booking {BookingId}", session.Id, bookingId);
                    return session;
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Error creating Stripe checkout session for booking {BookingId}", bookingId);
                    throw new PaymentServiceException($"Failed to create checkout session for booking {bookingId}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error creating checkout session for booking {BookingId}", bookingId);
                    throw new PaymentServiceException($"Unexpected error creating checkout session for booking {bookingId}", ex);
                }
            }

            public async Task<Session> CreateCheckoutSessionAsync(int amount, int bookingId, string currency = "usd")
            {
                try
                {
                    var options = new SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = amount, // Amount in cents
                                Currency = currency,
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Booking #{bookingId}",
                                },
                            },
                            Quantity = 1,
                        },
                    },
                        Mode = "payment",
                        SuccessUrl = _configuration["Stripe:SuccessUrl"] ?? "http://localhost:4200/success",
                        CancelUrl = _configuration["Stripe:CancelUrl"] ?? "http://localhost:4200/cancel",
                        Metadata = new Dictionary<string, string>
                    {
                        { "BookingId", bookingId.ToString() }
                    }
                    };

                    var service = new SessionService();
                    var session = await service.CreateAsync(options);
                    _logger.LogInformation("Stripe Checkout Session created: {SessionId}", session.Id);
                    return session;
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Error creating Stripe Checkout Session");
                    throw new PaymentServiceException("Failed to create Stripe Checkout Session", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error creating Stripe Checkout Session");
                    throw new PaymentServiceException("Unexpected error creating Stripe Checkout Session", ex);
                }
            }

            public async Task<Payment> RecordPaymentAsync(int bookingId, decimal amount, string paymentMethod, string transactionId, PaymentStatus status = PaymentStatus.Pending)
            {
                try
                {
                    var payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = amount,
                        PaymentMethod = paymentMethod,
                        TransactionId = transactionId,
                        Status = status,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdPayment = await _paymentRepository.AddAsync(payment);
                    _logger.LogInformation("Payment recorded: {PaymentId} for Booking {BookingId}", createdPayment.Id, bookingId);
                    return createdPayment;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recording payment for booking {BookingId}", bookingId);
                    throw new PaymentServiceException($"Failed to record payment for booking {bookingId}", ex);
                }
            }

            public async Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatus status)
            {
                try
                {
                    var payment = await _paymentRepository.GetByIdAsync(paymentId);
                    if (payment == null)
                    {
                        _logger.LogWarning("Payment {PaymentId} not found for status update", paymentId);
                        return false;
                    }

                    payment.Status = status;
                    payment.UpdatedAt = DateTime.UtcNow;

                    await _paymentRepository.UpdateAsync(payment);
                    _logger.LogInformation("Payment {PaymentId} status updated to {Status}", paymentId, status);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating payment status for payment {PaymentId}", paymentId);
                    throw new PaymentServiceException($"Failed to update payment status for payment {paymentId}", ex);
                }
            }

            public async Task<bool> InitiatePayFastPaymentAsync(int bookingId, decimal amount, string customerEmail)
            {
                try
                {
                    // Configuration is handled in PaymentController; this method is a placeholder for future API calls
                    _logger.LogInformation("PayFast payment initiated for Booking {BookingId}", bookingId);

                    // Record the payment attempt
                    await RecordPaymentAsync(bookingId, amount, "PayFast", $"payfast_{bookingId}_{DateTime.UtcNow:yyyyMMddHHmmss}", PaymentStatus.Pending);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initiating PayFast payment for Booking {BookingId}", bookingId);
                    return false;
                }
            }

            public async Task<bool> InitiatePayFastRefundAsync(int paymentId, decimal amount, int bookingId)
            {
                try
                {
                    var merchantId = _configuration["PayFast:MerchantId"] ?? "10000100";
                    var merchantKey = _configuration["PayFast:MerchantKey"] ?? "46f0cd694581a";
                    var passphrase = _configuration["PayFast:Passphrase"] ?? "test_passphrase";

                    var refundData = new Dictionary<string, string>
                {
                    { "merchant_id", merchantId },
                    { "merchant_key", merchantKey },
                    { "amount", amount.ToString("F2") },
                    { "m_payment_id", bookingId.ToString() }
                };

                    var signature = GenerateSignature(refundData, passphrase);
                    refundData.Add("signature", signature);

                    var content = new FormUrlEncodedContent(refundData);
                    var response = await _httpClient.PostAsync("https://api.payfast.co.za/refunds", content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("PayFast refund initiated for Payment {PaymentId}", paymentId);

                        // Update payment status to refunded
                        await UpdatePaymentStatusAsync(paymentId, PaymentStatus.Refunded);

                        return true;
                    }

                    _logger.LogWarning("PayFast refund failed for Payment {PaymentId}. Status: {StatusCode}", paymentId, response.StatusCode);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initiating PayFast refund for Payment {PaymentId}", paymentId);
                    return false;
                }
            }

            public async Task<bool> InitiateStripeRefundAsync(string paymentIntentId)
            {
                try
                {
                    var options = new RefundCreateOptions
                    {
                        PaymentIntent = paymentIntentId
                    };
                    var service = new RefundService();
                    var refund = await service.CreateAsync(options);

                    if (refund.Status == "succeeded")
                    {
                        _logger.LogInformation("Stripe refund succeeded for PaymentIntent {PaymentIntentId}", paymentIntentId);

                        // Find and update the payment record
                        var payments = await _paymentRepository.FindAsync(p => p.TransactionId == paymentIntentId);
                        var payment = payments.FirstOrDefault();
                        if (payment != null)
                        {
                            await UpdatePaymentStatusAsync(payment.Id, PaymentStatus.Refunded);
                        }

                        return true;
                    }

                    _logger.LogWarning("Stripe refund failed for PaymentIntent {PaymentIntentId}. Status: {Status}", paymentIntentId, refund.Status);
                    return false;
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Error initiating Stripe refund for PaymentIntent {PaymentIntentId}", paymentIntentId);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error initiating Stripe refund for PaymentIntent {PaymentIntentId}", paymentIntentId);
                    return false;
                }
            }

            public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
            {
                try
                {
                    var payments = await _paymentRepository.FindAsync(p => p.TransactionId == transactionId);
                    return payments.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting payment by transaction ID {TransactionId}", transactionId);
                    throw new PaymentServiceException($"Failed to get payment by transaction ID {transactionId}", ex);
                }
            }

            public async Task<List<Payment>> GetPaymentsByBookingIdAsync(int bookingId)
            {
                try
                {
                    var payments = await _paymentRepository.FindAsync(p => p.BookingId == bookingId);
                    return payments.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting payments for booking {BookingId}", bookingId);
                    throw new PaymentServiceException($"Failed to get payments for booking {bookingId}", ex);
                }
            }

            public string GenerateSignature(Dictionary<string, string> data, string passphrase)
            {
                var sorted = data.OrderBy(x => x.Key);
                var sb = new StringBuilder();
                foreach (var kvp in sorted)
                {
                    sb.Append($"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}&");
                }
                if (!string.IsNullOrEmpty(passphrase))
                    sb.Append($"passphrase={HttpUtility.UrlEncode(passphrase)}");
                else
                    sb.Length--;

                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            public string GenerateSignature(NameValueCollection data, string passphrase)
            {
                var sortedKeys = data.AllKeys.OrderBy(k => k).ToList();
                var sb = new StringBuilder();
                foreach (var key in sortedKeys)
                {
                    if (key != "signature")
                    {
                        sb.Append($"{key}={HttpUtility.UrlEncode(data[key])}&");
                    }
                }
                if (!string.IsNullOrEmpty(passphrase))
                    sb.Append($"passphrase={HttpUtility.UrlEncode(passphrase)}");
                else
                    sb.Length--;

                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        // Custom Exception Class
        public class PaymentServiceException : Exception
        {
            public PaymentServiceException(string message) : base(message) { }
            public PaymentServiceException(string message, Exception innerException) : base(message, innerException) { }
        }

    
}