using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MovieCatalog.Models;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Text.Json;
using static System.Net.WebRequestMethods;
namespace MovieCatalog
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string CreatedMovieId;

        private const string BaseURL = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI3MTg3ZTY0ZS00ZWQyLTRhNTQtODIwNS1iM2JjYjY3ZTEzNzEiLCJpYXQiOiIwNC8xOC8yMDI2IDA1OjU5OjIxIiwiVXNlcklkIjoiZmVjMmQwYjUtMzI4OC00NjE3LTYyMTUtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJRQTkyQGV4YW1wbGUuY29tIiwiVXNlck5hbWUiOiJRQXNvZnR1bmk5MiIsImV4cCI6MTc3NjUxMzU2MSwiaXNzIjoiTW92aWVDYXRhbG9nX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiTW92aWVDYXRhbG9nX1dlYkFQSV9Tb2Z0VW5pIn0.basOvEuK7fcTzqyK1aHv-qNVKrKbAPL71F4U8CYCg70";

        private const string LoginEmail = "QA92@example.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseURL)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };


            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseURL);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            var response = tempClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token.");
                }
                return token;

            }
            else
            {
                throw new InvalidOperationException($"Authentication failed with status code: {response.StatusCode}");
            }




        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            
            var newMovie = new 
            {
                
                Title = "Test Movie",
                Description = "This is a test movie.",
                PosterUrl = "",
                TrailerLink = "",
                IsWatched = true


            };
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(newMovie);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(content.Movie, Is.Not.Null, "Movie object should not be null.");
            Assert.That(content.Msg, Is.EqualTo("Movie created successfully!"));
            Assert.That(content.Movie.Id, Is.Not.Null.And.Not.Empty, "Movie ID should not be null or empty.");
            CreatedMovieId = content.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditMovie_WithValidData_ShouldReturnSuccess()
        {
            var updatedMovie = new
            {
                
                Title = "Updated Test Movie",
                Description = "This is an updated test movie.",
                PosterUrl = "",
                TrailerLink = "",
                IsWatched = true
            };
            var request = new RestRequest($"/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", CreatedMovieId);
            request.AddJsonBody(updatedMovie);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));

        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var movie = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(movie, Is.Not.Empty);





        }

        [Order(4)]
        [Test]
        public void DeleteMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", CreatedMovieId);
            var response = this.client.Execute(request);
            

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFields_ShouldReturnSuccessAgain()
        {
            var newMovie = new
            {
                PosterUrl = "",
                TrailerLink = "",
                IsWatched = true
            };
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(newMovie);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]
        public void EditNonExistentMovie_ShouldReturnNotFound()
        {
            string nonExistentMovieId = "123";
            var updatedMovie = new
            {
                Title = "Non-existent Movie",
                Description = "This movie does not exist.",
                PosterUrl = "",
                TrailerLink = "",
                IsWatched = true
            };
            var request = new RestRequest($"/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", "nonExistentMovieId");
            request.AddJsonBody(updatedMovie);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("Unable to edit the movie! Check the movieId parameter or user verification!"));


        }

        [Order(7)]
        [Test]
        public void DeleteNonExistentMovie_ShouldReturnNotFound()
        {
            string nonExistentMovieId = "123";
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistentMovieId);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(response.Content, Does.Contain("Unable to delete the movie! Check the movieId parameter or user verification"));
        }














        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
