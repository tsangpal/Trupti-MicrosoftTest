## mjml-hero

Display a section with a background image and some content inside (mj-text, mj-button, mj-image ...) wrapped in mj-hero-content component

Fixed height  

<p align="center">
  <img src="https://cloud.githubusercontent.com/assets/1830348/15354833/bfe7faaa-1cef-11e6-8d38-15e8951b6636.png" />
</p>

```xml
<mjml>
  <mj-body>
    <mj-container>
      <mj-hero
        mode="fixed-height"
        height="469px"
        background-width="600px"
        background-height="469px"
        background-url="https://cloud.githubusercontent.com/assets/1830348/15354890/1442159a-1cf0-11e6-92b1-b861dadf1750.jpg"
        background-color="#2a3448"
        padding="100px 0px">
        <!-- To add content like mj-image, mj-text, mj-button ... use the mj-hero-content component -->
        <mj-hero-content width="100%">
          <mj-text
            padding="20px"
            color="#ffffff"
            font-family="Helvetica"
            align="center"
            font-size="45"
            line-height="45px"
            font-weight="900">
            GO TO SPACE
          </mj-text>
          <mj-button href="https://mjml.io/" align="center">
            ORDER YOUR TICKET NOW
          </mj-button>
        </mj-hero-content>
      </mj-hero>
    </mj-container>
  </mj-body>
</mjml>
 ```

 <p align="center">
   <a href="https://mjml.io/try-it-live/components/hero">
     <img width="100px" src="http://imgh.us/TRYITLIVE.svg" alt="sexy" />
   </a>
 </p>

Fluid height

<p align="center">
  <img src="https://cloud.githubusercontent.com/assets/1830348/15354867/fc2f404a-1cef-11e6-92ac-92de9e438210.png" />
</p>

```xml
<mjml>
  <mj-body>
    <mj-container>
      <mj-hero
        mode="fluid-height"
        background-width="600px"
        background-height="469px"
        background-url="https://cloud.githubusercontent.com/assets/1830348/15354890/1442159a-1cf0-11e6-92b1-b861dadf1750.jpg"
        background-color="#2a3448"
        padding="100px 0px">
        <!-- To add content like mj-image, mj-text, mj-button ... use the mj-hero-content component -->
        <mj-hero-content width="100%">
          <mj-text
          padding="20px"
          color="#ffffff"
          font-family="Helvetica"
          align="center"
          font-size="45"
          line-height="45px"
          font-weight="900">
            GO TO SPACE
          </mj-text>
          <mj-button href="https://mjml.io/" align="center">
            ORDER YOUR TICKET NOW
          </mj-button>
        </mj-hero-content>
      </mj-hero>
    </mj-container>
  </mj-body>
</mjml>
```

<p align="center">
  <a href="https://mjml.io/try-it-live/components/hero/1">
    <img width="100px" src="http://imgh.us/TRYITLIVE.svg" alt="sexy" />
  </a>
</p>

<aside class="notice">
  The height attribute is required only for the fixed-height mode
</aside>

<aside class="notice">
  <span style="font-weight:bold;">The background position does not work in fluid-height mode on outlook.com</span>
</aside>

<aside class="notice">
For better result we encourage you to use a background image width equal to the hero container width and always specify a fallback background color, in case the user email client does not support background images.
</aside>

<aside class="notice">
  Please keep the hero container height below the image height. When the hero container height - both in fixed or fluid modes - is greater than the background image height, we can???t guarantee a perfect rendering in all supported email clients
</aside>

attribute           | unit                                | description                                                          | default value
--------------------|-------------------------------------|----------------------------------------------------------------------|--------------
width               | px                                  | hero container width                                                 | parent element width
mode                | fluid-height/fixed-height           | choose if the height is fixed based on the height attribute or fluid | fluid-height
height              | px                                  | hero section height (required for fixed-height mode)                 | 0px
background-width    | px                                  | width of the image used                                              | 0px
background-height   | px                                  | height of the image used                                             | 0px
background-url      | url                                 | absolute background url                                              | n/a
background-color    | color                               | hero background color                                                | #ffffff
background-position | top/center/bottom left/center/right | background image position                                            | center center
padding             | px                                  | supports up to 4 parameters                                          | 0px
padding-top         | px                                  | top offset                                                           | 0px
padding-right       | px                                  | right offset                                                         | 0px
padding-left        | px                                  | left offset                                                          | 0px
padding-bottom      | px                                  | bottom offset                                                        | 0px
vertical-align      | top/middle/bottom                   | content vertical alignment                                           | top

### mjml-hero-content

Display some content in an `mj-hero` component

<aside class="notice">
Use only one mj-hero-content component inside a mj-hero component
</aside>

attribute        | unit              | description                                    | default value
-----------------|-------------------|------------------------------------------------|------------------------------
width            | px/percent        | content width                                  | 100%
align            | left/center/right | horizontal alignment                           | center
background-color | color             | content background color                       | transparent
padding          | px                | supports up to 4 parameters                    | 0px
padding-top      | px                | top offset                                     | 0px
padding-right    | px                | right offset                                   | 0px
padding-left     | px                | left offset                                    | 0px
padding-bottom   | px                | bottom offset                                  | 0px
