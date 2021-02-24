using System;

namespace Home.Model
{
    public class ClientData
    {
        private static readonly string CLIENT_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.xml");
        public static ClientData Instance = ClientData.Load();        

        public string ClientID { get; set; } = Guid.NewGuid().ToString();

        public static ClientData Load()
        {
            try
            {
                var result = Serialization.Serialization.Read<ClientData>(CLIENT_PATH);
                if (result != null)
                    return result;
            }
            catch
            {

            }

            
            // ID should not changed!
            var cl = new ClientData();
            cl.Save();

            return cl;
        }


        public void Save()
        {
            try
            {
                Serialization.Serialization.Save<ClientData>(CLIENT_PATH, this);
            }
            catch
            {

            }
        }

    }
}
