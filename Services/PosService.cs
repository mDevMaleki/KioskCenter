using System.Net;
using KioskCenter.Interfaces;


namespace KioskCenter.Services
{


    public enum PosType
    {
        PardakhtNovin,
        Parsian

    }
    public class PosService : IPosService
    {
        private readonly PosParsianService _posParsianService;
        private readonly IPardakhtNovinService _pcPos;

        public PosService(IPardakhtNovinService pcPos, PosParsianService posParsianService)
        {
            _posParsianService = posParsianService;
            _pcPos = pcPos;
        }

        public async Task<string> sendToLan(decimal Amount, string IpAddress, int Port, PosType posType)
        {
            var result = "";

            switch (posType)
            {
                case PosType.PardakhtNovin:
                    _pcPos.SendToPos(Amount, IpAddress, Port);
                    break;

                case PosType.Parsian:
                    result = await _posParsianService.sendToLan(Amount, IpAddress, Port);
                    break;
            }

            return result;
        }
    }

}
