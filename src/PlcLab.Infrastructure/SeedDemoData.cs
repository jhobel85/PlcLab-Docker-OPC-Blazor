    
namespace PlcLab.Infrastructure
{      public static class SeedDemoData
    {
        // Replace these NodeIds with the actual NodeId strings from your OPC UA server
        public static readonly (string Label, string NodeId)[] Variables =
        [
            (PlcLabConstants.Enable_Static, "ns=6;s=Scalar_Static_Boolean"),
            (PlcLabConstants.Float_Static, "ns=6;s=Scalar_Static_Float"),
            (PlcLabConstants.Uint_Static, "ns=6;s=Scalar_Static_UInt32")
        ];

        public static readonly (string Label, string NodeId)[] Methods =
        [
            (PlcLabConstants.Method_Add, "ns=6;s=Methods_Add")
        ];
    }
}
