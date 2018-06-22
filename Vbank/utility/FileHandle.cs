using System;
using System.Collections.Generic;
using System.IO;
using Vbank.entity;

namespace Vbank.utility
{
    public class FileHandle
    {
        
        public static void readFile()
        {
            var list = new List<Transaction>();
            var lines = File.ReadAllLines(@"file/NeverEverGetBackTogether.txt");
            for (var i = 0; i < lines.Length; i+= 1)
            {
                if (i == 0)
                {
                    continue;
                }

                var lineSplited = lines[i].Split("|");
                if (lineSplited.Length == 8)
                {
                    var tx = new Transaction
                    {
                        Id = lineSplited[0],
                        SenderAccountNumber = lineSplited[1],
                        ReceiverAccountNumber = lineSplited[2],
                        Amount = Convert.ToDecimal(lineSplited[3]),
                        Content = lineSplited[4],
                        CreatedAt = lineSplited[5],
                        Status = (Transaction.ActiveStatus) Int32.Parse(lineSplited[6]),
                        Type = (Transaction.TransactionType) Int32.Parse(lineSplited[7]),
                    };
                    list.Add(tx);
                }
            }

            foreach (var tx in list)
            {
                Console.WriteLine(tx.Id + " " + tx.Amount);
            }
        }
    }
}