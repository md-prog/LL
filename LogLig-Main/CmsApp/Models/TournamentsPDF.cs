using AppModel;
using System;
using System.Collections.Generic;

namespace CmsApp.Models
{
    public class TournamentsPDF
    {
        public enum EditType
        {
            LgUnion = 0,
            TmntUnion = 1,
            TmntSectionClub = 2,
            TmntUnionClub = 3
        }

        private const int pdfCount = 4;
        public TournamentsPDF()
        {
            pdfArr = new string[4];
        }
        public int? UnionId { get; set; }
        public int SeasonId { get; set; }
        public int? ClubId { get; set; }
        public EditType Et { get; set; }
        public List<League> listLeagues { get; set; }
        private string[] pdfArr;
        public string this[int index]
        {
            get
            {
                if (index < 0 && index >= pdfCount)
                {
                    throw new IndexOutOfRangeException();
                }
                return pdfArr[index];
            }
            set
            {
                if (index < 0 || index >= pdfCount)
                {
                    throw new IndexOutOfRangeException();
                }
                pdfArr[index] = value;
            }
        }
        public int Count { get { return pdfCount; } }
        public string Pdf1 { get { return pdfArr[0]; } set { pdfArr[0] = value; } }
        public string Pdf2 { get { return pdfArr[1]; } set { pdfArr[1] = value; } }
        public string Pdf3 { get { return pdfArr[2]; } set { pdfArr[2] = value; } }
        public string Pdf4 { get { return pdfArr[3]; } set { pdfArr[3] = value; } }
    }
}