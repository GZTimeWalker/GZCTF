import { generateColors } from '@mantine/colors-generator'
import { Text, ColorInput, Group, Modal, ModalProps, Radio, Stack, InputBase, useMantineTheme } from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ColorPreview } from '@Components/ColorPreview'
import { ColorProvider, CustomColor, useCustomColor } from '@Utils/ThemeOverride'
import misc from '@Styles/Misc.module.css'

const colorRegex = /^#[0-9A-F]{6}$/i

export const CustomColorModal: FC<ModalProps> = (props) => {
  const { customColor, setCustomColor } = useCustomColor()
  const [color, setColor] = useState<CustomColor>(customColor || '')
  const [debouncedColor] = useDebouncedValue(color, 200)
  const theme = useMantineTheme()

  const { t } = useTranslation()

  const valid = colorRegex.test(color.color)

  const colors = color.provider === ColorProvider.Custom && valid ? generateColors(color.color) : theme.colors.brand

  useEffect(() => {
    setColor(customColor)
  }, [customColor])

  useEffect(() => {
    setCustomColor(debouncedColor)
  }, [debouncedColor])

  return (
    <Modal title={t('common.content.color.title')} {...props}>
      <Stack>
        <Radio.Group
          label={t('common.content.color.provider.label')}
          description={t('common.content.color.provider.description')}
          value={color.provider}
          onChange={(e) => {
            setColor({ ...color, provider: e as ColorProvider })
          }}
        >
          <Group justify="space-around" mt="xs">
            {Object.keys(ColorProvider).map((p) => (
              <Radio
                key={p}
                value={p}
                label={
                  <Text size="sm" fw="bold">
                    {t(`common.content.color.provider.${p.toLowerCase()}`)}
                  </Text>
                }
              />
            ))}
          </Group>
        </Radio.Group>
        <InputBase
          label={t('common.content.color.palette.label')}
          description={t('common.content.color.palette.description')}
          variant="unstyled"
          disabled={color.provider !== ColorProvider.Custom}
          component={ColorPreview}
          h="100%"
          colors={colors}
          displayColorsInfo={false}
          classNames={{ input: misc.flex }}
        />
        <ColorInput
          label={t('common.content.color.custom.label')}
          description={t('common.content.color.custom.description')}
          placeholder={t('common.content.color.custom.placeholder')}
          disabled={color.provider !== ColorProvider.Custom}
          error={color.provider === ColorProvider.Custom && !valid}
          value={color.provider === ColorProvider.Custom ? color.color : ''}
          onChange={(value) => setColor({ ...color, color: value })}
        />
      </Stack>
    </Modal>
  )
}
