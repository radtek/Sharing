﻿

namespace Sharing.Core.Tests.Models {
	using Sharing.Core.Models.Excel;
	[ExcelSheet(SheetName = "产品分类")]
	public class CategroyExcelBulkRow : ExcelBulkEditRow {

		[ExcelColumn(HeaderText = "编号", ReadOnly = true)]
		public long Id { get; set; }

		[ExcelColumn(HeaderText = "商户编号")]
		public long MerchantId { get; set; }

		[ExcelColumn(HeaderText = "分类名称")]
		public string Name { get; set; }

		[ExcelColumn(HeaderText = "备注")]
		public string Description { get; set; }


	}

	[ExcelSheet(SheetName = "商品信息")]
	public class ProductExcelBulkRow : ExcelBulkEditRow {

		[ExcelColumn(HeaderText = "商品编号", ReadOnly = true)]
		public long Id { get; set; }

		[ExcelColumn(ExcelEditInputType = ExcelEditInputTypes.DropDownList, HeaderText = "商户编号")]
		public long MerchantId { get; set; }

		[ExcelColumn(ExcelEditInputType = ExcelEditInputTypes.DropDownList, HeaderText = "商品类别")]
		public long CategoryId { get; set; }

		[ExcelColumn(HeaderText = "商品名称")]
		public string Name { get; set; }

		[ExcelColumn(HeaderText = "单价")]
		public int Price { get; set; }

		[ExcelColumn(HeaderText = "销量")]
		public int SalesVol { get; set; }

		[ExcelColumn(HeaderText = "排序号")]
		public int SortNo { get; set; }

		[ExcelColumn(ExcelEditInputType = ExcelEditInputTypes.DropDownList, HeaderText = "是否启用")]
		public bool Enabled { get; set; }

		[ExcelColumn(HeaderText = "封面图片")]
		public string ImageUrl { get; set; }

		[ExcelColumn(HeaderText = "备注")]
		public string Description { get; set; }

		public string Settings { get; set; }

	}
	[ExcelSheet(SheetName = "商品规格属性")]
	public class ProductOptionExcelBultEditRow : ExcelBulkEditRow {

		[ExcelColumn(HeaderText = "商品编号")]
		public long ProductId { get; set; }

		[ExcelColumn(HeaderText = "商品名称")]
		public string Product { get; set; }

		[ExcelColumn(HeaderText = "规格名称")]
		public string Name { get; set; }

		[ExcelColumn(HeaderText = "加价金额")]
		public int Price { get; set; }

		[ExcelColumn(HeaderText = "选项")]
		public string Item { get; set; }
	}
}