import { forwardRef } from 'react'
import { Group, MantineColor, Stack, Text, useMantineTheme } from '@mantine/core'
import {
  mdiBomb,
  mdiBullhornOutline,
  mdiCellphoneCog,
  mdiChevronTripleLeft,
  mdiChip,
  mdiConsole,
  mdiEthereum,
  mdiFingerprint,
  mdiFlag,
  mdiGamepadVariantOutline,
  mdiHexagonSlice2,
  mdiHexagonSlice4,
  mdiHexagonSlice6,
  mdiLightbulbOnOutline,
  mdiMatrix,
  mdiPlus,
  mdiWeb,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { ChallengeTag, ChallengeType, NoticeType, SubmissionType } from '@Api'

export const ChallengeTypeLabelMap = new Map<ChallengeType, ChallengeTypeItemProps>([
  [ChallengeType.StaticAttachment, { label: '静态附件', desrc: '共用附件，任意 flag 均可提交' }],
  [ChallengeType.StaticContainer, { label: '静态容器', desrc: '共用容器，任意 flag 均可提交' }],
  [
    ChallengeType.DynamicAttachment,
    { label: '动态附件', desrc: '按照队伍分发附件，需保证附件数量大于队伍数' },
  ],
  [ChallengeType.DynamicContainer, { label: '动态容器', desrc: '自动生成下发 flag，每队均唯一' }],
])

export interface ChallengeTypeItemProps extends React.ComponentPropsWithoutRef<'div'> {
  label: string
  desrc: string
}

export const ChallengeTypeItem = forwardRef<HTMLDivElement, ChallengeTypeItemProps>(
  ({ label, desrc, ...others }: ChallengeTypeItemProps, ref) => {
    return (
      <Stack spacing={0} ref={ref} {...others}>
        <Text size="sm">{label}</Text>
        <Text size="xs">{desrc}</Text>
      </Stack>
    )
  }
)

export const ChallengeTagLabelMap = new Map<ChallengeTag, ChallengeTagItemProps>([
  [
    ChallengeTag.Misc,
    { desrc: '杂项', icon: mdiGamepadVariantOutline, label: ChallengeTag.Misc, color: 'teal' },
  ],
  [
    ChallengeTag.Crypto,
    { desrc: '密码学', icon: mdiMatrix, label: ChallengeTag.Crypto, color: 'indigo' },
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

export const BloodsTypes = [
  SubmissionType.FirstBlood,
  SubmissionType.SecondBlood,
  SubmissionType.ThirdBlood,
]

export const SubmissionTypeIconMap = (size: number) => {
  const theme = useMantineTheme()
  return new Map([
    [SubmissionType.Unaccepted, undefined],
    [
      SubmissionType.Normal,
      <Icon
        path={mdiFlag}
        size={size}
        color={theme.colors.brand[theme.colorScheme === 'dark' ? 4 : 7]}
      />,
    ],
    [
      SubmissionType.FirstBlood,
      <Icon path={mdiHexagonSlice6} size={size} color={theme.colors.yellow[5]} />,
    ],
    [
      SubmissionType.SecondBlood,
      <Icon
        path={mdiHexagonSlice4}
        size={size}
        color={theme.fn.lighten(theme.colors.gray[2], 0.3)}
      />,
    ],
    [
      SubmissionType.ThirdBlood,
      <Icon
        path={mdiHexagonSlice2}
        size={size}
        color={theme.fn.darken(theme.colors.orange[7], theme.colorScheme === 'dark' ? 0.25 : 0.1)}
      />,
    ],
  ])
}

export const NoticTypeIconMap = (size: number) => {
  const theme = useMantineTheme()
  const submissionMap = SubmissionTypeIconMap(size)
  const colorIdx = theme.colorScheme === 'dark' ? 4 : 7

  return new Map([
    [
      NoticeType.Normal,
      <Icon path={mdiBullhornOutline} size={size} color={theme.colors.brand[colorIdx]} />,
    ],
    [
      NoticeType.NewHint,
      <Icon path={mdiLightbulbOnOutline} size={size} color={theme.colors.yellow[colorIdx]} />,
    ],
    [
      NoticeType.NewChallenge,
      <Icon path={mdiPlus} size={size} color={theme.colors.green[colorIdx]} />,
    ],
    [NoticeType.FirstBlood, submissionMap.get(SubmissionType.FirstBlood)],
    [NoticeType.SecondBlood, submissionMap.get(SubmissionType.SecondBlood)],
    [NoticeType.ThirdBlood, submissionMap.get(SubmissionType.ThirdBlood)],
  ])
}
