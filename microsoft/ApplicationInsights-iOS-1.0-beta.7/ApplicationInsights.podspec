Pod::Spec.new do |s|
  s.name             = "ApplicationInsights"
  s.deprecated       = true
  s.version          = "1.0-beta.7"
  s.summary          = "Microsoft Application Insights SDK for iOS"
  s.description      = <<-DESC
                       Application Insights is a service that allows developers to keep their applications available, performant, and successful. 
                       This SDK will allow you to send telemetry of various kinds (event, trace, exception, etc.) and useful crash reports to the Application Insights service where they can be visualized in the Azure Portal.
                       DESC
  s.homepage         = "https://github.com/Microsoft/ApplicationInsights-iOS/"
  s.license          = { :type => 'MIT', :file => 'ApplicationInsights/LICENSE' }
  s.author           = { "Microsoft" => "appinsights-ios@microsoft.com" }

  s.source           = { :http => "https://github.com/Microsoft/ApplicationInsights-iOS/releases/download/v#{s.version}/ApplicationInsights-#{s.version}.zip" }

  s.platform        = :ios, '6.0'
  s.requires_arc    = true

  s.frameworks      = 'CoreTelephony', 'UIKit', 'Foundation', 'SystemConfiguration', 'Security'
  s.libraries       = 'z'

  s.preserve_path   = 'ApplicationInsights/README.md'
  s.vendored_frameworks = 'ApplicationInsights/ApplicationInsights/ApplicationInsights.framework'
end
