using System;
using System.IO;
using MapWpf;
using MapWpf.Google;


namespace MapWpf.Properties
{
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings
    {

        // ReSharper disable EmptyConstructor
        public Settings()
        {
            // ReSharper restore EmptyConstructor
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }
        
        public static string GetMapFileName(GoogleBlock block)
        {
            var mapPath = Directory.GetCurrentDirectory() + Default.MapCacheLocalPath;
            //if (!Path.IsPathRooted(mapPath))
            //{
            //    mapPath = Path.Combine(
            //        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            //        mapPath);
            //}
            var fileName = Path.Combine(mapPath, block.Level + "\\" + (block.X / 100) + "_" + (block.Y / 100) + "\\" + block.Level + "_" + block.X + "_" + block.Y + ".png");

            return fileName;
        }

        private static Coordinate _centerMapBound;

        public static Coordinate CenterMapBound
        {
            get
            {
                if (_centerMapBound == null)
                {
                    var rectBound = new CoordinateRectangle(Default.LeftMapBound, Default.TopMapBound, Default.RightMapBound, Default.BottomMapBound);
                    _centerMapBound = rectBound.LineMiddlePoint;
                }
                return _centerMapBound;
            }
        }
    }
}
