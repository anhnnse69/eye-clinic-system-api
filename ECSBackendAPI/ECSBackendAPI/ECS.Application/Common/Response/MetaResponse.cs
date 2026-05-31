namespace ECS.Application.Common.Response
{
    public class MetaResponse
    {
        // page
        public int Page { get; }

        // size
        public int Size { get; }

        // total
        public int Total { get; }

        // total_pages
        public int TotalPages => (int)Math.Ceiling((double)Total / Size);

        // has_next
        public bool HasNext => Page < TotalPages;

        // has_previous
        public bool HasPrevious => Page > 1;

        /// <summary>
        /// Meta response constructor
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="total"></param>
        public MetaResponse(int page, int size, int total = 0)
        {
            Page = page;
            Size = size;
            Total = total;
        }
    }
}
