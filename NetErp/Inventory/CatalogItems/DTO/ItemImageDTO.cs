using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NetErp.Inventory.CatalogItems.DTO
{
    public class ItemImageDTO: Screen, ICloneable
    {
		private string _id;

		public string Id
		{
			get { return _id; }
			set 
			{
				if (_id != value) 
				{
					_id = value;
					NotifyOfPropertyChange(nameof(Id));
				}
			}
		}

		private int _itemId;

		public int ItemId
		{
			get { return _itemId; }
			set 
			{
				if (_itemId != value) 
				{
					_itemId = value;
					NotifyOfPropertyChange(nameof(ItemId));
				}
			}
		}

		private string _s3Bucket;

		public string S3Bucket
		{
			get { return _s3Bucket; }
			set 
			{
				if (_s3Bucket != value)
				{
					_s3Bucket = value;
					NotifyOfPropertyChange(nameof(S3Bucket));
				}
			}
		}

		private string _s3BucketDirectory;

		public string S3BucketDirectory
		{
			get { return _s3BucketDirectory; }
			set 
			{
				if (_s3BucketDirectory != value)
				{
					_s3BucketDirectory = value;
					NotifyOfPropertyChange(nameof(S3BucketDirectory));
				}
			}
		}

		private string _s3FileName;

		public string S3FileName
		{
			get { return _s3FileName; }
			set 
			{
				if (_s3FileName != value)
				{
					_s3FileName = value;
					NotifyOfPropertyChange(nameof(S3FileName));
				}
			}
		}

		private int _order;

		public int Order
		{
			get { return _order; }
			set 
			{
				if (_order != value)
				{
                    _order = value;
					NotifyOfPropertyChange(nameof(Order));
				}
			}
		}

		private string _imagePath;

		public string ImagePath
		{
			get { return _imagePath; }
			set 
			{
				if (_imagePath != value)
				{
					_imagePath = value;
					NotifyOfPropertyChange(nameof(ImagePath));
				}
			}
		}


		private BitmapImage _sourceImage;

		public BitmapImage SourceImage
		{
			get { return _sourceImage; }
			set 
			{
				if (_sourceImage != value)
				{
					_sourceImage = value;
					NotifyOfPropertyChange(nameof(SourceImage));
				}
			}
		}

		private bool _isSelected;

		public bool IsSelected
		{
			get { return _isSelected; }
			set { _isSelected = value; }
		}


		public object Clone()
        {
			return this.MemberwiseClone();
        }
    }
}
