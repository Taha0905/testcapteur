using System;
using System.Text;
using System.Windows;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace testcapteur
{
    public partial class MainWindow : Window
    {
        private MqttClient client;
        private DateTime lastSoundUpdate = DateTime.MinValue; // Pour suivre la dernière mise à jour du son
        private TimeSpan soundUpdateInterval = TimeSpan.FromSeconds(1); // Limite à 1 seconde entre chaque mise à jour du son

        public MainWindow()
        {
            InitializeComponent();

            // Connexion au broker MQTT
            string brokerAddress = "172.31.254.123"; // L'adresse IP de votre broker MQTT
            client = new MqttClient(brokerAddress);

            // Abonnement à l'événement de réception de message
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId, "Taha", "Taha"); // Utilisateur et mot de passe si nécessaire

            // Abonnement à plusieurs topics pour chaque capteur
            client.Subscribe(new string[] {
                "Batiment_3/1er/KM_102/Afficheur_n_1/Capteur_temperature_et_humidité",
                "Batiment_3/1er/KM_102/Afficheur_n_1/Capteur_de_CO2",
                "Batiment_3/1er/KM_102/Afficheur_n_1/Capteur_de_son"
            },
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        }

        // Méthode appelée lorsque vous recevez un message MQTT
        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);

            // Extrait les données en fonction du topic reçu
            string topic = e.Topic;

            Dispatcher.Invoke(() =>
            {
                if (topic.Contains("Capteur_temperature_et_humidité"))
                {
                    // Extraction des données de température en Celsius et de l'humidité
                    string temperature = ExtractValue(message, "Temp", "C");
                    string humidity = ExtractValue(message, "Humidity", "%");

                    // Mise à jour des TextBlocks pour la température et l'humidité
                    TemperatureTextBlock.Text = $"Température: {temperature} °C";
                    HumidityTextBlock.Text = $"Humidité: {humidity}%";
                }
                else if (topic.Contains("Capteur_de_CO2"))
                {
                    // Extraction des données de CO2 (PM2.5 et PM10)
                    string pm25 = ExtractValue(message, "PM2.5", "microg/m³");
                    string pm10 = ExtractValue(message, "PM10", "microg/m³");

                    // Mise à jour des TextBlocks pour le CO2
                    CO2TextBlock.Text = $"PM2.5: {pm25} µg/m³, PM10: {pm10} µg/m³";
                }
                else if (topic.Contains("Capteur_de_son"))
                {
                    // Vérifier si assez de temps s'est écoulé depuis la dernière mise à jour du son
                    if (DateTime.Now - lastSoundUpdate > soundUpdateInterval)
                    {
                        string sound = ExtractValue(message, "Capteur_de_son =", "dB");

                        // Mise à jour des TextBlocks pour le son seulement si assez de temps est passé
                        SoundTextBlock.Text = $"Son: {sound} dB";

                        // Mise à jour du temps de la dernière mise à jour du son
                        lastSoundUpdate = DateTime.Now;
                    }
                }
            });
        }

        // Méthode pour extraire une valeur d'un message avec un délimiteur spécifique
        private string ExtractValue(string message, string key, string delimiter)
        {
            try
            {
                if (message.Contains(key) && message.Contains(delimiter))
                {
                    int startIndex = message.IndexOf(key) + key.Length + 1;
                    int endIndex = message.IndexOf(delimiter, startIndex);

                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        return message.Substring(startIndex, endIndex - startIndex).Replace(delimiter, "").Trim();
                    }
                }

                // Retourne "N/A" si la clé ou le délimiteur ne sont pas trouvés
                return "N/A";
            }
            catch (Exception ex)
            {
                // Affiche un popup avec le message d'erreur
                MessageBox.Show($"Erreur lors de l'extraction des données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return "N/A"; // Retourne "N/A" en cas d'erreur
            }
        }
    }
}
