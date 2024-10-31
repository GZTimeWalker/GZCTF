import { generateColors } from '@mantine/colors-generator';
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
  Button,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { FC, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ColorProvider, CustomColor, useCustomColor } from '@Utils/ThemeOverride';
import ColorPreview from './ColorPreview';

const CustomColorModal: FC<ModalProps> = (props) => {
  const { customColor, setCustomColor } = useCustomColor();
  const [color, setColor] = useState<CustomColor>(customColor || '');
  const [tempColor, setTempColor] = useState(color.color); // 暂存用户输入的颜色
  const [debouncedColor] = useDebouncedValue(color, 200);
  const theme = useMantineTheme();
  const { t } = useTranslation();
  const [error, setError] = useState(''); // 用于存储错误信息

  const colors =
    color.provider === ColorProvider.Custom && /^#[0-9A-F]{6}$/i.test(color.color)
      ? generateColors(color.color)
      : theme.colors.brand;

  useEffect(() => {
    setColor(customColor);
    setTempColor(customColor.color); // 设置初始的暂存颜色
  }, [customColor]);

  useEffect(() => {
    setCustomColor(debouncedColor);
  }, [debouncedColor]);

  // 校验 HEX 颜色值的函数
  const isValidHex = (hex) => {
    const regex = /^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$/;
    return regex.test(hex);
  };

  const handleConfirm = () => {
    if (isValidHex(tempColor)) {
      setColor({ ...color, color: tempColor });
      setError(''); // 清除错误信息
    } else {
      setError('请输入有效的 HEX 颜色值'); // 设置错误信息
    }
  };

  return (
    <Modal title={t('common.content.color.title')} {...props}>
      <Stack>
        <Radio.Group
          label={t('common.content.color.provider.label')}
          description={t('common.content.color.provider.description')}
          value={color.provider}
          onChange={(e) => {
            setColor({ ...color, provider: e as ColorProvider });
            setTempColor(''); // 清空暂存颜色
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
          disabled={color.provider !== ColorProvider.Custom}
          value={tempColor} // 使用暂存的颜色
          onChange={(value) => setTempColor(value)} // 更新暂存的颜色
        />
        {error && <Text color="red">{error}</Text>} {/* 显示错误信息 */}
        <Button onClick={handleConfirm}>确认</Button> {/* 添加确认按钮 */}
      </Stack>
    </Modal>
  );
};

export default CustomColorModal;
