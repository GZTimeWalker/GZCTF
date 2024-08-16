import { generateColors } from '@mantine/colors-generator'
import {
  Text,
  ColorInput,
  Group,
  Modal,
  ModalProps,
  Radio,
  Stack,
  InputBase,
  useMantineTheme,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useCustomColor } from '@Utils/ThemeOverride'
import ColorPreview from './ColorPreview'

enum ColorProvider {
  Managed = 'Managed',
  Default = 'Default',
  Custom = 'Custom',
}

const CustomColorModal: FC<ModalProps> = (props) => {
  const { color: storedColor, setCustomColor } = useCustomColor()
  const [color, setColor] = useState<string>(storedColor || '')
  const [debouncedColor] = useDebouncedValue(color, 200)
  const theme = useMantineTheme()

  const [provider, setProvider] = useState<ColorProvider>(
    storedColor
      ? storedColor === 'brand'
        ? ColorProvider.Default
        : ColorProvider.Custom
      : ColorProvider.Managed
  )

  const { t } = useTranslation()

  const colors =
    provider === ColorProvider.Custom && /^#[0-9A-F]{6}$/i.test(color)
      ? generateColors(color)
      : theme.colors.brand

  useEffect(() => {
    if (provider === ColorProvider.Custom && /^#[0-9A-F]{6}$/i.test(debouncedColor)) {
      setCustomColor(debouncedColor)
    } else if (provider === ColorProvider.Managed) {
      setCustomColor('')
    } else {
      setCustomColor('brand')
    }
  }, [debouncedColor, provider, setCustomColor])

  return (
    <Modal title={t('common.content.color.title')} {...props}>
      <Stack>
        <Radio.Group
          label={t('common.content.color.provider.label')}
          description={t('common.content.color.provider.description')}
          value={provider}
          onChange={(e) => {
            setProvider(e as ColorProvider)
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
          disabled={provider !== ColorProvider.Custom}
          component={ColorPreview}
          h="100%"
          colors={colors}
          displayColorsInfo={false}
          styles={{
            input: {
              display: 'flex',
            },
          }}
        />
        <ColorInput
          label={t('common.content.color.custom.label')}
          description={t('common.content.color.custom.description')}
          placeholder={t('common.content.color.custom.placeholder')}
          disabled={provider !== ColorProvider.Custom}
          value={color && color !== 'brand' ? color : ''}
          onChange={setColor}
        />
      </Stack>
    </Modal>
  )
}

export default CustomColorModal
