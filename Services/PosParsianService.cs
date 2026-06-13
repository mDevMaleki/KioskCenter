using System.Net.Sockets;
using System.Text;

namespace KioskCenter.Services
{
    public  class PosParsianService
    {
        readonly   string publicKeyModule = "be8eece2b435b413efd8648076cbee8c20c2308da90c3624b589a8d399e85fa694e2ba619a501995a7f4d84120e4c001274eeac01ed1e3b2687a0e3e98634fb92f2ba425c22e1336c56b9e31d89b9000c51874f38f537d2a4ba4b2b5449bea2cf5be774440657bb9bd064523c38136ef93a3f8aa7ef8e712d25be3851f546582122a895636e903acab93361adce9d17e330512892dfa773ac75a47df6c95460207290401a9b6c19cbea90e400a06efaa25b161af1906339eb68e08de447c2d7c8476500820d532fd6b5ec53a2c958b2164c4dab89571ab41edbaa7f22197dae8ce25ef35851380785b816d6d1a2a627be945f14fb566a5c4d7359f69d80cb141";
        readonly  string publicKeyExp = "25";
                  
        readonly  string privateKeyModule = "a6c91fd48e66deb2f75f4faf222987c8bfe64f4d8178c9e3ac46b8902d7ee86d2a1aa3b33ec5f8d654e5a527c21ed266ac61a6813f460a0187d29c6490331f31a7c7a1b73c9280eb2def205ab87597b84f34c609875f27ba2ed657bd637177e5fe947af0014b5e9218b9e334bb7df30f459c5eb5b2250709d23d548d2761d8a493043edc79bce37c7c64928cdc2e05e6419bae10ca7371b14e237eb734d730928eca3290a3bf436047971e72a4385b52699ad30464dd2b0c3a6492479f30e0a62b2c39cfd051eec21289d0e64349ca6dc1bb4a3283175b1762a46f9d959d8a75db12f91cc9e248bfeb6664d49ebe7c379c211f0e65159c1938b55931d933eda5";
        readonly  string privateKeyExp = "25";
        readonly  string p = "cfb03c39c9d59590d992bc142db78f5fce3dd2b73c8a6a0b1d699bfe624ee7f213785a13d3903c5c62b69e0e647d00ce06d7212b660f1f4fcf7f8360c4ba44604212ee1947dd339b701df204fbf423f2bf56b62413acf3bf781fab974fb2c6d73e8c9fab3e2dde9bf773e98b015f68a9a0a6890aa12d2cd3f7b7e92c54212d93";
        readonly  string q = "cd95250365888a86384c23236f00f3309913f14d1fc801da18ff47694bcfd2a9e043a5766a21612af25336bdee798ee446d3ad9202b45c942a5931901b3259ada5d91571113dfacf6e6c2a279bbaa8b4bcfa7c9efce99e60650db4dda2febdbd4fbe00088dc1ee07ebc839d4701f44a5f3aaae462eb00817caef46e83b9dbae7";
        readonly  string dp = "8c547bb857f12db4e60932f8e09e9f250ecfd39090b08cd713df92ea26bfb17a0d27ce2912686022bf3d1eafc75b61689cd68c08911122fe931ece63fa8bb1a8d2b2d83a985730b5223dbf33ccd56465bf8d9da9ac6df0cd7aad9d74134171985ab20b05007fdb99d0b61a5700ed703b4308bd75e2864eb8bc225170fa92f547";
        readonly  string dq = "8ae83b9a82dfb78b1f1eaff558d720dba5b3875d9fda2ac3cbb3679a2c4e268e74ef6fcc8ce61f0f2e1c8cc58574ba7e90b88a08b5b822e0a6fdfee4d41b19ffb53fa6b42e3ea28c2803e521ae695d3bd9a2543412abb0334b32c65090b9f5d2f0b0cf9713ac8c13309c0b660691aaecab8f0021952ad5092840cf0b9de71d6b";
        readonly  string qInv = "cd0b59bd3792821c91f39d8d482463aaa334ab92b8990933cce997ea378cffebbda982b1d4c271385b6036c899296fe424ee682f3ecdc4c7e81a5bedc18344c0dc65034d0462311c0a1579c4e291bc11e8c1aae7199e029efbbe4600a872aa75397a352856579f2227410556c0a954e51e285169073698eb6df4bdbff68877eb";
        readonly  string d = "1207e7c3f3b818135903628f7950990ec8a346d7f2523f56e23128b5a40db8432725797b296182af631fbed3dda2779c65a9b122babb76b40eaefc268c2133ce044ceee3607e8a7a4a27b07882a4edcebc74686fbb9b9597965c5c832d585ffd3047673c8a84b04725448743f897fe99de031128664931317e832bbc3b9be0119e06cbd9de86c9040b84136329747ce6afd0a3ae511853a75c82e05b9fdc97219438e2ebef5b6be01a38c9b11d8a928dd981bb45f1bd966537691102758ad6c45a4c218af8219840c759ea063c10c17d37efcb2ea4984d13d713ca49951298da16237f499ee9c83a61bf6105bfb3df0453df1ff8d1dc95885fca2df5540ca69d";

        

        private  string errMsg = "";
        private  Thread tReader;
        private  TcpClient client = null;
        public  string sendToLan(decimal Amount)
        {
            string msg = "{\"cmd\":10,\"amount\":"+Amount.ToString()+", \"service\":\"000000\",\"sign\":\"899|123456789\"}";
            try
            {
                errMsg = "Closing Thread Error";
                if (tReader != null && tReader.ThreadState == ThreadState.Running) { tReader.Abort(); }
                errMsg = "Closing Client Error";
                if (client != null) { client.Close(); }
                errMsg = "Creating Client Error";
                client = new TcpClient("192.168.123.105", 1362);
                errMsg = "Creating NetworkStream Error";
                NetworkStream nwStream = client.GetStream();
                errMsg = "Creating Message Error";
                msg = msg.Length.ToString().PadLeft(4, '0') + msg;
                byte[] bytes = Encoding.ASCII.GetBytes(msg);
                nwStream.ReadTimeout = 60 * 1000;
                nwStream.WriteTimeout = 60 * 1000;
                errMsg = "Writing Message Error";
                nwStream.Write(bytes, 0, bytes.Length);
                //nwStream.Flush();
                errMsg = "Creating ReadLANResponse Error";
                try
                {
                    if (nwStream.CanRead)
                    {
                        byte[] bytesToRead = new byte[512];
                        int i;
                        string data = "";
                        errMsg = "Reading Error";
                        while ((i = nwStream.Read(bytesToRead, 0, bytesToRead.Length)) != 0)
                        {
                            data += Encoding.ASCII.GetString(bytesToRead, 0, i);
                            if (data.Length >= 4)
                            {
                                if (data.Length == int.Parse(data.Substring(0, 4)) + 4)
                                    break;
                            }
                        }

                        errMsg = "Closing Client Error in ReadLANResponse";
                        if (client != null) client.Close();
                        errMsg = "Closing NetworkStream Error in ReadLANResponse";
                        if (nwStream != null) nwStream.Close();
                        string retVal = data.Substring(4);

                        return retVal;
                    }
                    return "";
                }
                catch (Exception ex)
                {
                    if (client != null) client.Close();
                    if (nwStream != null) nwStream.Close();
                    return ex.Message;
                }


               
            }
            catch (Exception ex)
            {
                if (client != null) client.Close();
               return  ex.Message;
            }
        }
       



       

    }
}
