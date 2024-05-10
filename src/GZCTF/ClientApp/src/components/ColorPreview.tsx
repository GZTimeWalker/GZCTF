import { ColorSwatch, Group, GroupProps, MantineColorsTuple, isLightColor } from '@mantine/core'
import chroma from 'chroma-js'
import { FC } from 'react'
import classes from '@Styles/ColorPreview.module.css'

interface ColorsPreviewProps extends GroupProps {
  colors: MantineColorsTuple
  displayColorsInfo: boolean | undefined
}

const ColorPreview: FC<ColorsPreviewProps> = ({ colors, displayColorsInfo, ...others }) => {
  const colorList = colors.map((color) => chroma(color))

  const items = colorList.map((color, index) => (
    <div key={index} className={classes.item}>
      <ColorSwatch
        color={color.hex()}
        radius={0}
        className={classes.swatch}
        withShadow={false}
        c={isLightColor(color.hex()) ? 'black' : 'white'}
      >
        {displayColorsInfo && (
          <div className={classes.label}>
            <span className={classes.hex}>{color.hex()}</span>
          </div>
        )}
      </ColorSwatch>
    </div>
  ))

  return (
    <Group gap={0} wrap="nowrap" {...others}>
      {items}
    </Group>
  )
}

export default ColorPreview
