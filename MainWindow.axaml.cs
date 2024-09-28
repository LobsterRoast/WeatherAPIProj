using Avalonia.Controls;
using System.Reactive;
using ReactiveUI;
using Avalonia.Threading;
using Avalonia.ReactiveUI;
using Avalonia.Media.Imaging;
using System.Net.Http;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System;
using System.Threading.Tasks;
using ScottPlot.Avalonia;
using ScottPlot;


namespace WeatherAPIProj;

public partial class MainWindow : Window
{
    // JsonElement to hold API data
    public JsonElement root;

    // Declares a command binding usable in the XAML code
    public ReactiveCommand<Unit, Unit> CloseApp { get; }
    public List<double> historicalTemperatures = new List<double>();
    public List<int> yearsOfData = new List<int>();
    public double[] forecastedMaxTemps = new double[7];
    public double[] forecastedMinTemps = new double[7];

    // City and region are displayed in the application. Lat/Long are used to make API calls. These variables hold all their respective values found via. ipinfo.io.
    string? city;
    string? region;
    float? latitude;
    float? longitude;

    // Variables to hold weather data
    double? currentTemperature;
    double? dailyHigh;
    double? dailyLow;
    double? windSpeed;
    string? qualitativePrecipitation;

    // Variables to control the graphs 
    AvaPlot apHistoricalPlot;
    AvaPlot apForecastPlot;

    // Class to represent the window that the user sees
    public MainWindow()
    {
        InitializeComponent();

        // Defines the command binding we made earlier. When the command is run, it will execute CloseAppFunction()
        CloseApp = ReactiveCommand.Create(CloseAppFunction);
        DataContext = this;
        
        // Initializes the graphs in code
        InitializeGraphs();
        StylizeGraphs();

        apHistoricalPlot.Refresh();
        apForecastPlot.Refresh();
        // Gets the current date to use in API calls. The year of startDate is set to 1940 because that is when the earliest weather data the API can provide is from.
        

        // These methods have to run asynchronously to avoid displaying null values.
        Dispatcher.UIThread.Post(async () => 
        {
            // Makes a request to ipinfo.io to get location data (including geoposition)
            await RunHttpRequest("http://ipinfo.io/json");
            // Parses the json data received into the variables declared earlier
            await ParseIPInfoJson();
            // Makes a request to the Weather API using the latitude and longitude we received before
            await RunHttpRequest(
                "https://api.open-meteo.com/v1/forecast?latitude="+latitude+"&longitude="+longitude+
                "&current=temperature_2m,wind_speed_10m&daily=temperature_2m_max,temperature_2m_min,weather_code"
            );
            // Parses the weather data we just received into our weather variables
            await ParseWeatherJson();
            // Sets the text blocks declared in XAML to display the weather data
            SetTextBlocks();
            DateTime dtCurrentDate = DateTime.Today;
            for (int i = 1940 + ((dtCurrentDate.Year-1) % 10); i <= (dtCurrentDate.Year-1); i += 5) {
                string date = new DateTime(i, dtCurrentDate.Month, dtCurrentDate.Day).ToString("yyyy-MM-dd");
                Console.WriteLine("Fetching weather data from " + date + "...");
                await RunHttpRequest(
                    "https://archive-api.open-meteo.com/v1/archive?latitude="+latitude+"&longitude="+longitude+
                    "&start_date="+date+"&end_date="+date+"&daily=temperature_2m_mean"
                );
                historicalTemperatures.Add(root.GetProperty("daily").GetProperty("temperature_2m_mean")[0].GetDouble());
                yearsOfData.Add(i);
            } 
            Console.WriteLine("Done obtaining historical data.");
            SetHistoricalPlot();
            await RunHttpRequest(
                "https://api.open-meteo.com/v1/forecast?latitude="+latitude+"&longitude="+longitude+
                "&daily=temperature_2m_max,temperature_2m_min"
            );
            SetForecastPlot();
        });
    }


    int SetHistoricalPlot() {
        apHistoricalPlot.Plot.Axes.SetLimits(yearsOfData[0], yearsOfData[yearsOfData.Count-1], historicalTemperatures[0], historicalTemperatures[historicalTemperatures.Count-1]);
        apHistoricalPlot.Plot.Title("Yearly temperature at this time since " + yearsOfData[0]);
        apHistoricalPlot.Plot.Axes.SetLimitsY(-50, 50);
        apHistoricalPlot.Plot.Axes.AutoScaleExpandY();
        apHistoricalPlot.Plot.Add.Scatter(yearsOfData, historicalTemperatures);
        double[] dYearsOfData = yearsOfData.Select(i => (double)i).ToArray();
        string[] sYearsOfData = yearsOfData.Select(i => i.ToString()).ToArray();
        apHistoricalPlot.Plot.Axes.Bottom.SetTicks(dYearsOfData, sYearsOfData);
        apHistoricalPlot.Refresh();
        return 0;
    }
    int InitializeGraphs() {
        apHistoricalPlot = this.Find<AvaPlot>("historicalPlot");
        apForecastPlot = this.Find<AvaPlot>("forecastPlot");
        apHistoricalPlot.Plot.Title("Making API calls. This may take a minute...");
        apForecastPlot.Plot.Title("Making API calls. This may take a minute...");
        return 0;
    }
    int StylizeGraphs() {
        // Change the Forecast Plot to use dates as its X axis
        apForecastPlot.Plot.Axes.DateTimeTicksBottom();

        // Change the frame and interior colors of the plots
        apHistoricalPlot.Plot.FigureBackground.Color = Color.FromHex("#333333");
        apHistoricalPlot.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");
        apForecastPlot.Plot.FigureBackground.Color = Color.FromHex("#333333");
        apForecastPlot.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");

        // Change the colors of the axes and grid
        apHistoricalPlot.Plot.Axes.Color(Color.FromHex("#eeeeee"));
        apHistoricalPlot.Plot.Grid.MajorLineColor = Color.FromHex("#404040");
        apForecastPlot.Plot.Axes.Color(Color.FromHex("#eeeeee"));
        apForecastPlot.Plot.Grid.MajorLineColor = Color.FromHex("#404040");

        // Change the colors of the legends
        apHistoricalPlot.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
        apHistoricalPlot.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
        apHistoricalPlot.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");
        apForecastPlot.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
        apForecastPlot.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
        apForecastPlot.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

        return 0;
    }

    int SetForecastPlot() {
        DateTime dtCurrentDate = DateTime.Today;
        DateTime[] dates = {
            dtCurrentDate,
            dtCurrentDate.AddDays(1),
            dtCurrentDate.AddDays(2),
            dtCurrentDate.AddDays(3),
            dtCurrentDate.AddDays(4),
            dtCurrentDate.AddDays(5),
            dtCurrentDate.AddDays(6)
        };
        for (int i = 0; i < 7; i++) {
            forecastedMaxTemps[i] = root.GetProperty("daily").GetProperty("temperature_2m_max")[i].GetDouble();
            forecastedMinTemps[i] = root.GetProperty("daily").GetProperty("temperature_2m_min")[i].GetDouble();
        }
        
        apForecastPlot.Plot.Axes.SetLimitsY(-50, 50);
        apForecastPlot.Plot.Axes.AutoScaleExpandY();
        var min = apForecastPlot.Plot.Add.Scatter(dates, forecastedMinTemps);
        min.LegendText = "Min";
        var max = apForecastPlot.Plot.Add.Scatter(dates, forecastedMaxTemps);
        max.LegendText = "Max";
        apForecastPlot.Plot.Axes.AutoScaleX();
        apForecastPlot.Plot.Title("7 day forecast for this week (highs and lows)");
        apForecastPlot.Refresh();
        return 0;
    }

    void CloseAppFunction() {
        // Closes the application
       this.Close();
    }
    async Task RunHttpRequest(string url) {
        // Create an http client from which to make requests
        var httpClient = new HttpClient();
        // Send the request
        HttpResponseMessage response = await httpClient.GetAsync(url);
        // Receive the content and parse it into the JsonElement variable
        HttpContent content = response.Content;
        using (var responseStream = await response.Content.ReadAsStreamAsync()) {
            JsonDocument json = await JsonDocument.ParseAsync(responseStream);
            root = json.RootElement;
        }
    }
    async Task ParseIPInfoJson() {
        city = root.GetProperty("city").GetString();
        region = root.GetProperty("region").GetString();
        // Coordinates have to be handled in a goofy way because of how the json is organized
        string[] coordinates = root.GetProperty("loc").GetString().Split(',');
        latitude = StrToFloat(coordinates[0]);
        longitude = StrToFloat(coordinates[1]);
    }
    async Task ParseWeatherJson() {
        // Json library doesn't have a GetFloat function because it's evil
        JsonElement current = root.GetProperty("current");
        JsonElement daily = root.GetProperty("daily");
        currentTemperature = current.GetProperty("temperature_2m").GetDouble();
        dailyHigh = daily.GetProperty("temperature_2m_max")[0].GetDouble();
        dailyLow = daily.GetProperty("temperature_2m_max")[1].GetDouble();
        windSpeed = current.GetProperty("wind_speed_10m").GetDouble();
        // The weather_code property is simply an integer; They are WMO codes that correspond to certain weather conditions. The function below handles the parsing.
        WeatherCodeToVisualData(daily.GetProperty("weather_code")[0].GetInt32());
    }
    float StrToFloat(string inputString) {
        // C# doesn't have a good built in method to convert strings to floats :(
        if (string.IsNullOrEmpty(inputString)) {
            Console.WriteLine("Error! Unable to parse string into floating point. String is empty.");
            return -1;
        }
        float outputFloat;
        if (!float.TryParse(inputString, out outputFloat)) {
            Console.WriteLine("Error! Unable to parse string into floating point. String is invalid.");
            return 1;
        }
        return outputFloat;
    }
    int WeatherCodeToVisualData(int weatherCode) {
        // Sets the visual data to match the WMO code received from the API.
        switch (weatherCode) {
            case 0: case 1: case 2: case 3:
                qualitativePrecipitation = "Mostly clear";
                image.Source = new Bitmap("Emojis/sun_3d.png");
                break;
            case 45: case 48:
                qualitativePrecipitation = "Foggy";
                image.Source = new Bitmap("Emojis/fog_3d.png");
                break;
            case 51: case 53: case 55: case 56: case 57:
                qualitativePrecipitation = "Drizzly";
                image.Source = new Bitmap("Emojis/umbrella_with_rain_drops_3d.png");
                break;
            case 61: case 63: case 65: case 66: case 67: case 80: case 81: case 82:
                qualitativePrecipitation = "Rainy";
                image.Source = new Bitmap("Emojis/cloud_with_rain_3d.png");
                break;
            case 71: case 73: case 75: case 77: case 85: case 86:
                qualitativePrecipitation = "Snowy";
                image.Source = new Bitmap("Emojis/cloud_with_snow_3d.png");
                break;
            case 95: case 96: case 99:
                qualitativePrecipitation = "Thunderstorm";
                image.Source = new Bitmap("Emojis/cloud_with_lightning_and_rain_3d.png");
                break;
            default:
                // If for some reason the code doesn't have a WMO translation. we display a picture of our Lord Gaben.
                qualitativePrecipitation = "Could not parse weather code";
                image.Source = new Bitmap("gabe.jpg");
                break;
                
        }
        return 0;
    }
    int SetTextBlocks() {
        // Sets all of the XAML Text Blocks to display our information
        locationBlock.Text = "City: " + city + ", " + region;
        precipitationBlock.Text = "General Weather Condition: " + qualitativePrecipitation;
        tempBlock.Text = "Temperature: " + currentTemperature + "°C";
        highBlock.Text = "Daily High: " + dailyHigh + "°C";
        lowBlock.Text = "Daily Low: " + dailyLow + "°C";
        windBlock.Text = "Wind Speed: " + windSpeed + " kmph";
        return 0;
    }
}
