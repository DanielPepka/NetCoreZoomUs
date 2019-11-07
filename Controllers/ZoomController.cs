using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AndcultureCode.ZoomClient;
using AndcultureCode.ZoomClient.Models;
using AndcultureCode.ZoomClient.Models.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ZoomUsTests.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ZoomController : ControllerBase
	{
		static readonly char[] padding = { '=' };
		public static long ToTimestamp(DateTime value)
		{
			long epoch = (value.Ticks - 621355968000000000) / 10000;
			return epoch;
		}

		public static string GenerateToken(string apiKey, string apiSecret, string meetingNumber, string ts, string role)
		{
			string message = String.Format("{0}{1}{2}{3}", apiKey, meetingNumber, ts, role);
			apiSecret = apiSecret ?? "";
			var encoding = new System.Text.ASCIIEncoding();
			byte[] keyByte = encoding.GetBytes(apiSecret);
			byte[] messageBytes = encoding.GetBytes(message);

			using (var hmacsha256 = new HMACSHA256(keyByte))
			{
				byte[] key1 = Encoding.ASCII.GetBytes(apiSecret);
				byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
				string msgHash = System.Convert.ToBase64String(hashmessage);
				string token = String.Format("{0}.{1}.{2}.{3}.{4}", apiKey, meetingNumber, ts, role, msgHash);
				var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
				return System.Convert.ToBase64String(tokenBytes).TrimEnd(padding);
			}
		}

		// POST: api/Zoom
		[HttpGet]
		[Route("~/api/Zoom/Meeting")]
		public MeetingResponse Meeting([FromQuery]MeetingRequest request)
		{
			MeetingResponse output = new MeetingResponse() { Messages = new List<string>() };

			var options = new ZoomClientOptions
			{
				ZoomApiKey = "TODO - SET THIS",
				ZoomApiSecret = "TODO - SET THIS"
			};

			if (string.IsNullOrWhiteSpace(request.MeetingNumber))
			{
				var client = new ZoomClient(options);
				var allUsers = client.Users.GetUsers(UserStatuses.Active, 30, 1);

				var user = allUsers.Users.Single(u => u.Email == "TODO - SET THIS");

				output.Messages.Add("Found User: " + user.Id + " (" + user.FirstName + " " + user.LastName + ") - " + user.Email);

				var meeting = client.Meetings.CreateMeeting(user.Id, new AndcultureCode.ZoomClient.Models.Meetings.Meeting()
				{
					Topic = "string",
					Type = AndcultureCode.ZoomClient.Models.Meetings.MeetingTypes.Scheduled,
					StartTime = DateTime.Now,
					Duration = 30,
					Timezone = "America/Los_Angeles",
					Password = "",
					Agenda = "What is an agenda?",
					Recurrence = null,
					Settings = new AndcultureCode.ZoomClient.Models.Meetings.MeetingSettings() { EnableJoinBeforeHost = true }
				});

				request.MeetingNumber = meeting.Id;
			}

			string meetingNumber = request.MeetingNumber;
			string ts = ToTimestamp(DateTime.UtcNow.ToUniversalTime()).ToString();
			string role = "0";
			string token = GenerateToken(options.ZoomApiKey, options.ZoomApiSecret, meetingNumber, ts, role);

			output.ZoomToken = token;
			output.MeetingNumber = request.MeetingNumber;
			output.ApiSecret = options.ZoomApiSecret;
			output.ApiKey = options.ZoomApiKey;

			return output;
		}
	}

	public class MeetingRequest
	{
		[FromQuery]
		public string ApiKey { get; set; }
		[FromQuery]
		public string MeetingNumber { get; set; }
		[FromQuery]
		public string UserName { get; set; }
		[FromQuery]
		public string PassWord { get; set; }

	}

	public class MeetingResponse
	{
		public List<string> Messages { get; set; }
		public string ZoomToken { get; set; }
		public string MeetingNumber { get; set; }
		public string ApiSecret { get; set; }
		public string ApiKey { get; set; }
	}
}
