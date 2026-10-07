using System;
using System.IO;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Reports;
using Xunit;

namespace RhinoWood.Tests
{
    public class SheetsTests
    {
        private static (WoodProject p, ProjectResult r) T() { var p = WoodProject.CreateTable("Masă stejar"); return (p, p.Recalculate()); }

        [Fact]
        public void DesignSet_HasTechnicalJointCuttingAndAssemblySheets_SeparateSheets()
        {
            var (p, r) = T();
            var sheets = SheetBuilder.Build(p, r, SheetMode.Design);
            Assert.Equal("tech", sheets[0].Id);
            Assert.True(sheets[0].IsLivePreview);
            Assert.Contains(sheets, s => s.Id.StartsWith("joints"));
            Assert.Contains(sheets, s => s.Id.StartsWith("cut"));
            Assert.Contains(sheets, s => s.Id.StartsWith("cutlist"));
            Assert.Equal("assembly", sheets.Last().Id);
            // technical sheet carries views + materials but NOT the cutting plan, joint details or assembly notes
            Assert.Contains("VEDERE DE SUS", sheets[0].Body); Assert.Contains("MATERIALE", sheets[0].Body); Assert.Contains("Scara 1:", sheets[0].Body);
            Assert.DoesNotContain("plan de debitare", sheets[0].Body);
            Assert.DoesNotContain("Ordinea de montaj", sheets[0].Body);
        }

        [Fact]
        public void TechnicalSheet_HasOverallDimensions_AndRomanianNames()
        {
            var (p, r) = T();
            var body = SheetBuilder.TechnicalSheet(p, r, SheetMode.Design, null).Body;
            Assert.Contains(">1800<", body); Assert.Contains(">900<", body); Assert.Contains(">760<", body);
            Assert.Contains("Picior", body); Assert.Contains("Zargă lungă", body); Assert.Contains("Stejar", body);
            Assert.Contains("4 buc · 725×80×80", body);
        }

        [Fact]
        public void SaleSet_NeverContainsInternalCostsWasteOrStockCodes()
        {
            var (p, r) = T();
            var sheets = SheetBuilder.Build(p, r, SheetMode.Sale, "STANDARD");
            Assert.Equal(new[] { "tech", "offer" }, sheets.Select(s => s.Id).ToArray());
            var all = string.Join("\n", sheets.Select(s => s.Body));
            foreach (var forbidden in new[] { "deșeu", "Deșeu", "Cost producție", "Manoperă", "OAK-40x140", "debitare", "B001", "Necesar lucrare" })
                Assert.DoesNotContain(forbidden, all);
            Assert.Contains("Preț", all);
            // sale price = (direct cost + overhead) / (1 - margin on price), rounded up to 10, then VAT
            var pb = RhinoWood.Core.Projects.Pricing.Compute(p, r);
            Assert.Contains(Money.Format(pb.PriceExVat, r.Cost.Currency), all);
            Assert.Contains(Money.Format(pb.PriceIncVat, r.Cost.Currency), all);
            Assert.Contains("TVA", all);
        }

        [Fact]
        public void CuttingSheets_ShowWasteInRed_AndEveryPieceOnce()
        {
            var (p, r) = T();
            var sheets = SheetBuilder.Build(p, r).Where(s => s.Id.StartsWith("cut") || s.Id == "order").ToList();
            var all = string.Join("\n", sheets.Select(s => s.Body));
            Assert.Contains("#c23a22", all);
            foreach (var d in r.Demands) Assert.Contains(d.PartId, all);
            Assert.Contains("Necesar lucrare", all);
            Assert.Contains("Cost producție", all);
        }

        [Fact]
        public void JointSheet_IsHonestAboutValidatedDomain_R3R5()
        {
            var (p, r) = T();
            var body = string.Join("\n", SheetBuilder.Build(p, r).Where(s => s.Id.StartsWith("joints")).Select(s => s.Body));
            Assert.Contains("necesită încercare", body);
            Assert.Contains("în afara domeniului validat", body);
            Assert.Contains("Cep-mortază", body);
        }

        [Fact]
        public void Document_HasOnePagePerSheet_AndPageNumbers()
        {
            var (p, r) = T();
            var sheets = SheetBuilder.Build(p, r);
            var html = SheetDocument.Html(sheets, "t");
            Assert.Equal(sheets.Count, System.Text.RegularExpressions.Regex.Matches(html, "<section class='sheet'").Count);
            Assert.Contains("Planșa 1 / " + sheets.Count, html);
            Assert.Contains("@page { size: 297mm 210mm", html);
        }

        [Fact]
        public void Money_UsesThinSpaces()
        {
            Assert.Equal("1 840 lei", Money.Format(1840, "lei"));
            Assert.Equal("962 lei", Money.Format(962.4, "lei"));
        }

        [Fact]
        public void PdfExport_WorksWhenABrowserIsAvailable_OtherwiseFallsBackToHtml()
        {
            var (p, r) = T();
            var dir = Path.Combine(Path.GetTempPath(), "rw_pdf_" + Guid.NewGuid().ToString("N"));
            try
            {
                var (ok, msg) = PdfExporter.ExportSheets(p, r, Path.Combine(dir, "x.pdf"));
                Assert.True(File.Exists(Path.Combine(dir, "x.html")));            // always saved
                if (PdfExporter.FindBrowser() != null) { Assert.True(ok, msg); Assert.True(new FileInfo(Path.Combine(dir, "x.pdf")).Length > 5000); }
                else { Assert.False(ok); Assert.Contains("HTML", msg); }
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public void RomanianIssueText_ForMovement()
        {
            var (p, r) = T();
            var info = r.Issues.First(i => i.Code == "MOVEMENT_INFO");
            Assert.Contains("mișcarea sezonieră estimată", Ro.Issue(info));
        }
    }
}
