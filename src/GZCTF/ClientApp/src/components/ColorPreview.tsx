import { ColorSwatch, Group, GroupProps, MantineColorsTuple, isLightColor } from '@mantine/core'
import { FC } from 'react'
import classes from '@Styles/ColorPreview.module.css'

interface ColorsPreviewProps extends GroupProps {
  colors: MantineColorsTuple
  displayColorsInfo: boolean | undefined
}

const ColorPreview: FC<ColorsPreviewProps> = ({ colors, displayColorsInfo, ...others }) => {
  const items = colors.map((color, index) => (
    <div key={index} className={classes.item}>
      <ColorSwatch
        color={color}
        radius={0}
        className={classes.swatch}
        withShadow={false}
        c={isLightColor(color) ? 'black' : 'white'}
      >
        {displayColorsInfo && (
          <div className={classes.label}>
            <span className={classes.hex}>{color}</span>
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
