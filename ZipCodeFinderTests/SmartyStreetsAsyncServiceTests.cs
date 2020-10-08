using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ZipCodeFinder.Services;

namespace ZipCodeFinderTests
{
    public class Tests
    {
        Mock<HttpMessageHandler> mockHandler;
        Mock<IHttpClientFactory> mockHttpClientFactory;
        Mock<IConfiguration> mockConfig;
        Mock<IConfigurationSection> mockAppSettings;

        IZipCodeService serviceUnderTest;

        private const string VALID_CITY = "Dallas";
        private const string VALID_STATE = "TX";
        private const string INVALID_STATE = "XX";
        private const string VALID_API_KEY = "1111111";
        private const string INVALID_API_KEY = "2222222";

        [SetUp]
        public void Setup()
        {
            mockHandler = new Mock<HttpMessageHandler>();
            mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockConfig = new Mock<IConfiguration>();
            mockAppSettings = new Mock<IConfigurationSection>();

            SetupMocks();

            serviceUnderTest = new SmartyStreetsAsyncService(mockHttpClientFactory.Object, mockConfig.Object);
        }

        [Test]
        public void CanCreateService()
        {
            Assert.IsNotNull(serviceUnderTest);
        }

        [Test]
        public async Task LookupZips_ValidInput_ReturnsZipCodes()
        {
            var zipCodes = await serviceUnderTest.LookupZipCodes(VALID_CITY, VALID_STATE);

            Assert.AreEqual(2, zipCodes.Count);

            mockHandler.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
               ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task LookupZips_InvalidState_ReturnsEmptyList()
        {
            var zipCodes = await serviceUnderTest.LookupZipCodes(VALID_CITY, INVALID_STATE);

            Assert.AreEqual(0, zipCodes.Count);

            mockHandler.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.Query.Contains(INVALID_STATE)),
               ItExpr.IsAny<CancellationToken>());
        }

        [Test]
        public async Task LookupZips_InvalidCredentials_ReturnsEmptyList()
        {
            mockAppSettings.Setup(s => s.Value).Returns(INVALID_API_KEY);

            var zipCodes = await serviceUnderTest.LookupZipCodes(VALID_CITY, VALID_STATE);

            Assert.AreEqual(0, zipCodes.Count);

            mockHandler.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.Query.Contains(INVALID_API_KEY)),
               ItExpr.IsAny<CancellationToken>());
        }

        private void SetupMocks()
        {
            //////////////////////////////////////////////////
            // SETUP MOCK CONFIGURATION
            //////////////////////////////////////////////////
            mockAppSettings.Setup(s => s.Value).Returns(VALID_API_KEY);
            mockConfig.Setup(c => c.GetSection("SmartyStreetsApiKey")).Returns(mockAppSettings.Object);

            //////////////////////////////////////////////////
            // SETUP RESPONSE FOR VALID INPUT
            //////////////////////////////////////////////////
            var validInputResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[{""input_index"":0,""status"":""valid_state"",""zipcodes"":[{""zipcode"":""11111""},{""zipcode"":""22222""}]}]"),
            };

            mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri.Query.Contains(VALID_STATE) && m.RequestUri.Query.Contains(VALID_API_KEY)),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(validInputResponse);

            //////////////////////////////////////////////////
            // SETUP RESPONSE FOR INAVLID INPUT
            //////////////////////////////////////////////////
            var invalidStateReponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"[{""input_index"":0,""status"":""invalid_state"",""reason"":""Invalid state name or abbreviation.""}]"),
            };

            mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri.Query.Contains(INVALID_STATE)),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(invalidStateReponse);

            //////////////////////////////////////////////////
            // SETUP RESPONSE FOR UNAUTHORIZED API KEY
            //////////////////////////////////////////////////
            var unauthorizedResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent(@"[{""input_index"":0,""status"":""unauthorized"",""reason"":""Provided credentials are not valid.""}]"),
            };

            mockHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri.Query.Contains(INVALID_API_KEY)),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(unauthorizedResponse);

            //////////////////////////////////////////////////
            // SETUP FACTORY TO RETURN OUR TEST HTTPCLIENT
            //////////////////////////////////////////////////
            var testHttpClient = new HttpClient(mockHandler.Object);
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(testHttpClient);
        }
    }
}
