using Swashbuckle.SwaggerUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{

    public class AssetServiceRecord
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string ServiceDate { get; set; }
        public string NextServiceDate { get; set; }
        public string Remark { get; set; }
        public string ServicedBy { get; set; }
        public string ApprovedBy { get; set; }
        public decimal ServiceCost { get; set; }

        // These will be uploaded files
        public HttpPostedFileBase Image { get; set; }
        public HttpPostedFileBase ServiceDoc { get; set; }
    }

    public class AssetServiceRecordResponse
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string ServiceDate { get; set; }
        public string NextServiceDate { get; set; }
        public string Image { get; set; }        // store URL or Base64 string
        public string Remark { get; set; }
        public string ServiceDoc { get; set; }   // store URL or Base64 string
        public decimal ServiceCost { get; set; }
        public string ServicedBy { get; set; }
        public string ApprovedBy { get; set; }
    }


    public class AssetDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AssetType { get; set; }
        public string Manufacturer { get; set; }
        public string AssetModel { get; set; }
        public string LastServiceDate { get; set; }
        public string NextServiceDate { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
        public string Location { get; set; }
        public string AssetImage { get; set; }
        public string AMCdoc { get; set; }
    }

    public class FMServiceResponseModel
    {
        public int AssetId { get; set; }
        public string ServiceDate { get; set; }
        public string NextServiceDate { get; set; }
        public string Remark { get; set; }
        public string ServicedBy { get; set; }
        public string ApprovedBy { get; set; }
        public decimal ServiceCost { get; set; }
        public HttpPostedFileBase Image { get; set; }
        public HttpPostedFileBase ServiceDoc { get; set; }
    }

}