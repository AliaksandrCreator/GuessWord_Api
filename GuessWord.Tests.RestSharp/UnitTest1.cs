/// Подключаем пространства имён .NET
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
// --- СТАРЫЙ ВАРИАНТ ---
// using System.Net.Http;               // работа с HttpClient
// --- НОВЫЙ ВАРИАНТ ---
using RestSharp; // RestSharp — библиотека для HTTP-запросов
using Microsoft.AspNetCore.Mvc.Testing; // для запуска API внутри тестов

namespace WordGameTests
{
    [TestFixture]
    public class ApiTests
    {
        // --- СТАРЫЙ ВАРИАНТ ---
        // private HttpClient _client = default!;
        // --- НОВЫЙ ВАРИАНТ ---
        private WebApplicationFactory<GuessWord.Api.Program> _factory = default!;
        private RestClient _client = default!;

        private const string TestUser = "testuser";
        private const string TestUser1 = "testuser1";
        private const string TestUser2 = "testuser2";

        [SetUp]
        public async Task SetUp()
        {
            // --- СТАРЫЙ ВАРИАНТ ---
            // _client = new HttpClient { BaseAddress = new Uri("http://localhost:5114") };

            // --- НОВЫЙ ВАРИАНТ ---
            _factory = new WebApplicationFactory<GuessWord.Api.Program>();
            _client = new RestClient(_factory.CreateClient());

            // чистим всех тестовых пользователей до теста
            await _client.DeleteAsync(new RestRequest($"/user?user={TestUser}"));
            await _client.DeleteAsync(new RestRequest($"/user?user={TestUser1}"));
            await _client.DeleteAsync(new RestRequest($"/user?user={TestUser2}"));
        }

        [TearDown]
        public async Task TearDown()
        {
            // --- СТАРЫЙ ВАРИАНТ ---
            // _client.Dispose();

            // --- НОВЫЙ ВАРИАНТ ---
            await _client.DeleteAsync(new RestRequest($"/user?user={TestUser}"));
            await _client.DeleteAsync(new RestRequest($"/user?user={TestUser1}"));
            await _client.DeleteAsync(new RestRequest($"/user?user={TestUser2}"));

            _client.Dispose();
            _factory.Dispose();
        }

        // --- Тест 1: проверяем эндпоинт /start ---
        [Test]
        public async Task StartGame_ReturnsSessionId()
        {
            var request = new RestRequest("/start", Method.Post);
            request.AddQueryParameter("user", TestUser);

            var response = await _client.ExecuteAsync(request);

            TestContext.WriteLine("Ответ сервера: " + response.Content);

            Assert.That(response.IsSuccessful, Is.True);
            Assert.That(response.Content, Does.Contain("ID сессии"));
        }

        // --- Тест 2: проверяем эндпоинт /guess ---
        [Test]
        public async Task GuessLetter_ReturnsResult()
        {
            var startRequest = new RestRequest("/start", Method.Post);
            startRequest.AddQueryParameter("user", TestUser);
            var startResponse = await _client.ExecuteAsync(startRequest);
            var id = ExtractId(startResponse.Content);

            var guessRequest = new RestRequest("/guess", Method.Post);
            guessRequest.AddQueryParameter("letter", "A");
            guessRequest.AddQueryParameter("id", id.ToString());

            var guessResponse = await _client.ExecuteAsync(guessRequest);

            TestContext.WriteLine("Ответ сервера: " + guessResponse.Content);

            Assert.That(guessResponse.IsSuccessful, Is.True);
            Assert.That(guessResponse.Content, Does.Contain("Буква").Or.Contain("Игра уже завершена"));
        }

        // --- Тест 3: проверяем эндпоинт /statistics ---
        [Test]
        public async Task Statistics_ReturnsList()
        {
            var startRequest = new RestRequest("/start", Method.Post);
            startRequest.AddQueryParameter("user", TestUser);
            await _client.ExecuteAsync(startRequest);

            var statsRequest = new RestRequest("/statistics", Method.Get);
            var statsResponse = await _client.ExecuteAsync(statsRequest);

            TestContext.WriteLine("Ответ сервера: " + statsResponse.Content);

            Assert.That(statsResponse.IsSuccessful, Is.True);
            Assert.That(statsResponse.Content, Does.Contain("Игра"));
        }

        // --- Тест 4: удаление только одного пользователя из двух ---
        [Test]
        public async Task DeleteUser_RemovesOnlySpecifiedUser()
        {
            var r1 = await _client.ExecuteAsync(new RestRequest("/start", Method.Post).AddQueryParameter("user", TestUser1));
            var r2 = await _client.ExecuteAsync(new RestRequest("/start", Method.Post).AddQueryParameter("user", TestUser2));
            Assert.That(r1.IsSuccessful && r2.IsSuccessful, Is.True);

            var statsBefore = await _client.ExecuteAsync(new RestRequest("/statistics", Method.Get));
            TestContext.WriteLine("Статистика до удаления: " + statsBefore.Content);
            Assert.That(statsBefore.Content, Does.Contain(TestUser1));
            Assert.That(statsBefore.Content, Does.Contain(TestUser2));

            var deleteResponse = await _client.ExecuteAsync(new RestRequest("/user", Method.Delete).AddQueryParameter("user", TestUser1));
            TestContext.WriteLine("Удаление: " + deleteResponse.Content);
            Assert.That(deleteResponse.IsSuccessful, Is.True);

            var statsAfter = await _client.ExecuteAsync(new RestRequest("/statistics", Method.Get));
            TestContext.WriteLine("Статистика после удаления: " + statsAfter.Content);
            Assert.That(statsAfter.Content, Does.Not.Contain(TestUser1));
            Assert.That(statsAfter.Content, Does.Contain(TestUser2));
        }

        // --- Вспомогательный метод ---
        private long ExtractId(string? content)
        {
            if (string.IsNullOrEmpty(content))
                throw new InvalidOperationException("Ответ пустой, ID не найден");

            var matches = Regex.Matches(content, @"\d+");
            if (matches.Count == 0)
                throw new InvalidOperationException("ID не найден: " + content);

            return long.Parse(matches[matches.Count - 1].Value);
        }
    }
}

