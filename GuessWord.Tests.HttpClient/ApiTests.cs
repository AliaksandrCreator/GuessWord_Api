// Подключаем пространства имён .NET
using System;                        // базовые типы и исключения
using System.Net.Http;               // работа с HttpClient
using System.Text.RegularExpressions;// регулярные выражения для извлечения ID
using System.Threading.Tasks;        // поддержка async/await
using NUnit.Framework;               // NUnit — фреймворк для тестов
using Microsoft.AspNetCore.Mvc.Testing; // новый пакет для запуска API внутри тестов

namespace WordGameTests
{
    [TestFixture]
    public class ApiTests
    {
        // --- СТАРЫЙ ВАРИАНТ ---
        // private HttpClient _client = default!;

        // --- НОВЫЙ ВАРИАНТ ---
        private WebApplicationFactory<GuessWord.Api.Program> _factory = default!;
        private HttpClient _client = default!;

        private const string TestUser = "testuser";
        private const string TestUser1 = "testuser1";
        private const string TestUser2 = "testuser2";

        // Очистка перед каждым тестом
        [SetUp]
        public async Task SetUp()
        {
            // --- СТАРЫЙ ВАРИАНТ ---
            // _client = new HttpClient
            // {
            //     BaseAddress = new Uri("http://localhost:5114") // выстави реальный порт твоего API
            // };

            // --- НОВЫЙ ВАРИАНТ ---
            _factory = new WebApplicationFactory<GuessWord.Api.Program>();
            _client = _factory.CreateClient();

            // чистим всех тестовых пользователей до теста
            await _client.DeleteAsync($"/user?user={TestUser}");
            await _client.DeleteAsync($"/user?user={TestUser1}");
            await _client.DeleteAsync($"/user?user={TestUser2}");
        }

        // Очистка после каждого теста
        [TearDown]
        public async Task TearDown()
        {
            // --- СТАРЫЙ ВАРИАНТ ---
            // _client.Dispose();

            // --- НОВЫЙ ВАРИАНТ ---
            await _client.DeleteAsync($"/user?user={TestUser}");
            await _client.DeleteAsync($"/user?user={TestUser1}");
            await _client.DeleteAsync($"/user?user={TestUser2}");

            _client.Dispose();
            _factory.Dispose();
        }

        // --- Тест 1: проверяем эндпоинт /start ---
        [Test]
        public async Task StartGame_ReturnsSessionId()
        {
            var response = await _client.PostAsync($"/start?user={TestUser}", null);
            var content = await response.Content.ReadAsStringAsync();

            TestContext.WriteLine("Ответ сервера: " + content);

            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(content, Does.Contain("ID сессии"));
        }

        // --- Тест 2: проверяем эндпоинт /guess ---
        [Test]
        public async Task GuessLetter_ReturnsResult()
        {
            var startResponse = await _client.PostAsync($"/start?user={TestUser}", null);
            var startContent = await startResponse.Content.ReadAsStringAsync();
            var id = ExtractId(startContent);

            var response = await _client.PostAsync($"/guess?letter=A&id={id}", null);
            var content = await response.Content.ReadAsStringAsync();

            TestContext.WriteLine("Ответ сервера: " + content);

            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(content, Does.Contain("Буква").Or.Contain("Игра уже завершена"));
        }

        // --- Тест 3: проверяем эндпоинт /statistics ---
        [Test]
        public async Task Statistics_ReturnsList()
        {
            await _client.PostAsync($"/start?user={TestUser}", null);

            var response = await _client.GetAsync("/statistics");
            var content = await response.Content.ReadAsStringAsync();

            TestContext.WriteLine("Ответ сервера: " + content);

            Assert.That(response.IsSuccessStatusCode, Is.True);
            Assert.That(content, Does.Contain("Игра"));
        }

        // --- Тест 4: удаление только одного пользователя из двух ---
        [Test]
        public async Task DeleteUser_RemovesOnlySpecifiedUser()
        {
            var r1 = await _client.PostAsync($"/start?user={TestUser1}", null);
            var r2 = await _client.PostAsync($"/start?user={TestUser2}", null);
            Assert.That(r1.IsSuccessStatusCode && r2.IsSuccessStatusCode, Is.True);

            var statsBefore = await _client.GetAsync("/statistics");
            var statsContentBefore = await statsBefore.Content.ReadAsStringAsync();
            TestContext.WriteLine("Статистика до удаления: " + statsContentBefore);

            Assert.That(statsContentBefore, Does.Contain(TestUser1));
            Assert.That(statsContentBefore, Does.Contain(TestUser2));

            var deleteResponse = await _client.DeleteAsync($"/user?user={TestUser1}");
            var deleteContent = await deleteResponse.Content.ReadAsStringAsync();
            TestContext.WriteLine("Удаление: " + deleteContent);
            Assert.That(deleteResponse.IsSuccessStatusCode, Is.True);

            var statsAfter = await _client.GetAsync("/statistics");
            var statsContentAfter = await statsAfter.Content.ReadAsStringAsync();
            TestContext.WriteLine("Статистика после удаления: " + statsContentAfter);

            Assert.That(statsContentAfter, Does.Not.Contain(TestUser1));
            Assert.That(statsContentAfter, Does.Contain(TestUser2));
        }

        // --- Вспомогательный метод: извлекаем последний числовой ID из текста ---
        private long ExtractId(string content)
        {
            var matches = Regex.Matches(content, @"\d+");
            if (matches.Count == 0)
                throw new InvalidOperationException("ID не найден: " + content);

            return long.Parse(matches[matches.Count - 1].Value);
        }
    }
}


