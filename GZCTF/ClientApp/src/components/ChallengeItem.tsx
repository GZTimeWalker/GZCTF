import { forwardRef } from 'react'
import { Group, MantineColor, Text, useMantineTheme } from '@mantine/core'
import {
  mdiBomb,
  mdiCellphoneCog,
  mdiChevronTripleLeft,
  mdiChip,
  mdiConsole,
  mdiEthereum,
  mdiFingerprint,
  mdiMatrix,
  mdiQrcode,
  mdiWeb,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { ChallengeTag, ChallengeType } from '../Api'

export const ChallengeTypeLabelMap = new Map<ChallengeType, string>([
  [ChallengeType.StaticAttachment, '静态附件题'],
  [ChallengeType.StaticContainer, '静态容器题'],
  [ChallengeType.DynamicAttachment, '动态附件题'],
  [ChallengeType.DynamicContainer, '动态容器题'],
])

export const ChallengeTagLabelMap = new Map<ChallengeTag, ChallengeTagItemProps>([
  [ChallengeTag.Misc, { desrc: '杂项', icon: mdiQrcode, label: ChallengeTag.Misc, color: 'teal' }],
  [
    ChallengeTag.Crypto,
    { desrc: '密码学', icon: mdiMatrix, label: ChallengeTag.Crypto, color: 'blue' },
  ],
  [ChallengeTag.Pwn, { desrc: 'Pwn', icon: mdiBomb, label: ChallengeTag.Pwn, color: 'red' }],
  [ChallengeTag.Web, { desrc: 'Web', icon: mdiWeb, label: ChallengeTag.Web, color: 'blue' }],
  [
    ChallengeTag.Reverse,
    { desrc: '逆向工程', icon: mdiChevronTripleLeft, label: ChallengeTag.Reverse, color: 'yellow' },
  ],
  [
    ChallengeTag.Blockchain,
    { desrc: '区块链', icon: mdiEthereum, label: ChallengeTag.Blockchain, color: 'lime' },
  ],
  [
    ChallengeTag.Forensics,
    { desrc: '取证', icon: mdiFingerprint, label: ChallengeTag.Forensics, color: 'cyan' },
  ],
  [
    ChallengeTag.Hardware,
    { desrc: '硬件', icon: mdiChip, label: ChallengeTag.Hardware, color: 'violet' },
  ],
  [
    ChallengeTag.Mobile,
    { desrc: '移动设备', icon: mdiCellphoneCog, label: ChallengeTag.Mobile, color: 'pink' },
  ],
  [ChallengeTag.PPC, { desrc: '编程', icon: mdiConsole, label: ChallengeTag.PPC, color: 'orange' }],
])

export interface ChallengeTagItemProps extends React.ComponentPropsWithoutRef<'div'> {
  label: ChallengeTag
  desrc: string
  icon: string
  color: MantineColor
}

export const ChallengeTagItem = forwardRef<HTMLDivElement, ChallengeTagItemProps>(
  ({ label, icon, color, ...others }: ChallengeTagItemProps, ref) => {
    const theme = useMantineTheme()
    return (
      <Group ref={ref} noWrap {...others}>
        <Icon color={theme.colors[color][4]} path={icon} size={1} />
        <Text size="sm">{label}</Text>
      </Group>
    )
  }
)
