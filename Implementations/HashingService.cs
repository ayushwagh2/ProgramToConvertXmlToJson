using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HashidsNet;
using ProgramToConvertXmlToJson.Services;


namespace ProgramToConvertXmlToJson.Implementations;
    public class HashingService : IHashingService
        {
            private readonly Hashids _hashids;

            public HashingService(string salt, int minLength, string alphabet)
            {
                _hashids = new Hashids(salt, minLength, alphabet);
            }


    public string Encode(int input)
    {
        return _hashids.Encode(input);
    }
}
