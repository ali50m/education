﻿namespace WPFSimpleHttpClient.Extensions
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.Threading.Tasks;
	using WPFSimpleHttpClient.HttpClientWrapper;

	public static class HttpApiClientExtension
	{
		public static async Task<IEnumerable<JToken>> GetDataListAsync(this HttpApiClient client, 
			string library, string method, object[] values)
		{
			IEnumerable<JToken> result = null;
			Uri uri = MakeJServerRequestUri(client, library, method, values);

			string data = await client.GetAsync(uri);

			if (!string.IsNullOrWhiteSpace(data))
			{
				// Parse the JSON string
				JObject jObject = null;
				try
				{
					jObject = JObject.Parse(data);
				}
				catch (Exception ex)
				{
					client.OnErrorOccured(new HttpErrorEventArgs(ex, string.Empty, "Invalid response format. JSON expected."));
				}

				// Get "data" object as enumerable
				try
				{
					result = GetDataList(jObject);
				}
				catch (Exception ex)
				{
					client.OnErrorOccured(new HttpErrorEventArgs(ex, string.Empty, "Invalid 'data' object."));
				}
			}

			return result;
		}

		public static async Task<DataTable> GetDataTableAsync(this HttpApiClient client,
			string library, string method, object[] values)
		{
			IEnumerable<JToken> data = null;

			try
			{
				data = await client.GetDataListAsync(library, method, values);
			}
			catch (Exception ex)
			{
				client.OnErrorOccured(new HttpErrorEventArgs(ex, string.Empty, "Invalid 'data' object."));
			}

			return JCollectionToDataTable(data);
		}

		private static DataTable JCollectionToDataTable(IEnumerable<JToken> collection)
		{
			DataTable result = null;

			if (collection != null)
			{
				var list = collection.Select(j => j.ToString(Formatting.None));

				string json = "[" + string.Join($",", list) + "]";

				result = JsonConvert.DeserializeObject<DataTable>(json);
			}

			return result;
		}

		private static IEnumerable<JToken> GetDataList(JObject jObject)
		{
			IEnumerable<JToken> result = null;

			if (jObject != null)
			{
				JToken jToken = null;

				if (jObject.TryGetValue("data", out jToken))
				{
					JArray array = (JArray)jToken;
					result = array as IEnumerable<JToken>;
				}
			}

			return result;
		}

		private static Uri MakeJServerRequestUri(HttpApiClient client, string library, string method, params object[] values)
		{
			string path = $"{client.GetRequestUriString("client/")}{library.ToLower()}.{method.ToLower()}?apikey=00000";

			if (values != null && values.Any())
			{
				int id = 1;
				var parameters = values.Select(i => $"p{id++}={i.ToString()}");
				path += $"&{string.Join("&", parameters)}";
			}

			Uri uri = null;
			Uri.TryCreate(path, UriKind.Absolute, out uri);

			return uri;
		}
	}
}
