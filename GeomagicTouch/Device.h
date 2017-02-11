#pragma once
#include "NativeDevice.h"
#include <msclr\marshal_cppstd.h>

using namespace System;
using namespace System::ComponentModel;
using namespace System::Timers;

namespace GeomagicTouch {
	public ref class Device : INotifyPropertyChanged
	{
	public:
		Device(String^ DeviceName);
		void Start();
		void Stop();
		void Update();
		void UpdateTransform();
		void LoadObj(String^ ObjectPath);
		property double X { double get(); }
		property double Y { double get(); }
		property double Z { double get(); }
		property double Theta1 { double get();  }
		property double Theta2 { double get();  }
		property double Theta3 { double get();  }
		property double R00 { double get(); }
		property double R01 { double get(); }
		property double R02 { double get(); }
		property double R10 { double get(); }
		property double R11 { double get(); }
		property double R12 { double get(); }
		property double R20 { double get(); }
		property double R21 { double get(); }
		property double R22 { double get(); }
		property bool Button1 { bool get();  }
		property bool Button2 { bool get();  }
		property bool Button3 { bool get();  }
		property bool Button4 { bool get();  }
		property bool IsInInkwell { bool get(); }
		
		property double SetpointX { double get(); void set(double value); }
		property double SetpointY { double get(); void set(double value); }
		property double SetpointZ { double get(); void set(double value); }
		property bool SetpointEnabled { bool get(); void set(bool value); }

		property String^ Name { String^ get() { return deviceName; }}
		virtual event PropertyChangedEventHandler^ PropertyChanged;
		property bool IsInitialized { bool get() { return device->deviceInitialized;  }}

	private:
		bool isRunning;
		String^ deviceName;
		NativeDevice *device;
		double x;
		double y;
		double z;
		double theta1;
		double theta2;
		double theta3;
		double r00;
		double r01;
		double r02;
		double r10;
		double r11;
		double r12;
		double r20;
		double r21;
		double r22;
		bool button1;
		bool button2;
		bool button3;
		bool button4;
		bool isInInkwell;
	};
}

