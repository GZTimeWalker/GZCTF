import { Group, MantineColor, Stack, Text, useMantineTheme } from '@mantine/core'
import {
  mdiBomb,
  mdiBullhornOutline,
  mdiCancel,
  mdiCellphoneCog,
  mdiCheck,
  mdiChevronTripleLeft,
  mdiChip,
  mdiClose,
  mdiConsole,
  mdiEthereum,
  mdiFingerprint,
  mdiFlag,
  mdiGamepadVariantOutline,
  mdiHelpCircleOutline,
  mdiHexagonSlice2,
  mdiHexagonSlice4,
  mdiHexagonSlice6,
  mdiLightbulbOnOutline,
  mdiMatrix,
  mdiPlus,
  mdiRobotLoveOutline,
  mdiWeb,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { forwardRef } from 'react'
import { useTranslation } from 'react-i18next'
import {
  ChallengeTag,
  ChallengeType,
  NoticeType,
  ParticipationStatus,
  SubmissionType,
  TaskStatus,
} from '@Api'

export const useChallengeTypeLabelMap = () => {
  const { t } = useTranslation()

  return new Map<ChallengeType, ChallengeTypeItemProps>([
    [
      ChallengeType.StaticAttachment,
      {
        label: t('challenge.type.static_attachment.label'),
        desrc: t('challenge.type.static_attachment.desrc'),
      },
    ],
    [
      ChallengeType.StaticContainer,
      {
        label: t('challenge.type.static_container.label'),
        desrc: t('challenge.type.static_container.desrc'),
      },
    ],
    [
      ChallengeType.DynamicAttachment,
      {
        label: t('challenge.type.dynamic_attachment.label'),
        desrc: t('challenge.type.dynamic_attachment.desrc'),
      },
    ],
    [
      ChallengeType.DynamicContainer,
      {
        label: t('challenge.type.dynamic_container.label'),
        desrc: t('challenge.type.dynamic_container.desrc'),
      },
    ],
  ])
}

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

export const useChallengeTagLabelMap = () => {
  const { t } = useTranslation()

  return new Map<ChallengeTag, ChallengeTagItemProps>([
    [
      ChallengeTag.Misc,
      {
        desrc: t('challenge.tag.misc'),
        icon: mdiGamepadVariantOutline,
        label: ChallengeTag.Misc,
        color: 'teal',
      },
    ],
    [
      ChallengeTag.Crypto,
      {
        desrc: t('challenge.tag.crypto'),
        icon: mdiMatrix,
        label: ChallengeTag.Crypto,
        color: 'indigo',
      },
    ],
    [
      ChallengeTag.Pwn,
      { desrc: t('challenge.tag.pwn'), icon: mdiBomb, label: ChallengeTag.Pwn, color: 'red' },
    ],
    [
      ChallengeTag.Web,
      { desrc: t('challenge.tag.web'), icon: mdiWeb, label: ChallengeTag.Web, color: 'blue' },
    ],
    [
      ChallengeTag.Reverse,
      {
        desrc: t('challenge.tag.reverse'),
        icon: mdiChevronTripleLeft,
        label: ChallengeTag.Reverse,
        color: 'yellow',
      },
    ],
    [
      ChallengeTag.Blockchain,
      {
        desrc: t('challenge.tag.blockchain'),
        icon: mdiEthereum,
        label: ChallengeTag.Blockchain,
        color: 'lime',
      },
    ],
    [
      ChallengeTag.Forensics,
      {
        desrc: t('challenge.tag.forensics'),
        icon: mdiFingerprint,
        label: ChallengeTag.Forensics,
        color: 'cyan',
      },
    ],
    [
      ChallengeTag.Hardware,
      {
        desrc: t('challenge.tag.hardware'),
        icon: mdiChip,
        label: ChallengeTag.Hardware,
        color: 'violet',
      },
    ],
    [
      ChallengeTag.Mobile,
      {
        desrc: t('challenge.tag.mobile'),
        icon: mdiCellphoneCog,
        label: ChallengeTag.Mobile,
        color: 'pink',
      },
    ],
    [
      ChallengeTag.PPC,
      {
        desrc: t('challenge.tag.ppc'),
        icon: mdiConsole,
        label: ChallengeTag.PPC,
        color: 'orange',
      },
    ],
    [
      ChallengeTag.AI,
      {
        desrc: t('challenge.tag.ai'),
        icon: mdiRobotLoveOutline,
        label: ChallengeTag.AI,
        color: 'green',
      },
    ]
  ])
}

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

export const SubmissionTypeColorMap = () => {
  const theme = useMantineTheme()
  return new Map([
    [SubmissionType.Unaccepted, undefined],
    [SubmissionType.Normal, theme.colors.brand[theme.colorScheme === 'dark' ? 8 : 6]],
    [SubmissionType.FirstBlood, theme.colors.yellow[5]],
    [
      SubmissionType.SecondBlood,
      theme.colorScheme === 'dark'
        ? theme.fn.lighten(theme.colors.gray[2], 0.3)
        : theme.fn.darken(theme.colors.gray[1], 0.2),
    ],
    [
      SubmissionType.ThirdBlood,
      theme.colorScheme === 'dark'
        ? theme.fn.darken(theme.colors.orange[7], 0.25)
        : theme.fn.lighten(theme.colors.orange[7], 0.2),
    ],
  ])
}

export const SubmissionTypeIconMap = (size: number) => {
  const colorMap = SubmissionTypeColorMap()
  return {
    iconMap: new Map([
      [SubmissionType.Unaccepted, undefined],
      [
        SubmissionType.Normal,
        <Icon path={mdiFlag} size={size} color={colorMap.get(SubmissionType.Normal)} />,
      ],
      [
        SubmissionType.FirstBlood,
        <Icon
          path={mdiHexagonSlice6}
          size={size}
          color={colorMap.get(SubmissionType.FirstBlood)}
        />,
      ],
      [
        SubmissionType.SecondBlood,
        <Icon
          path={mdiHexagonSlice4}
          size={size}
          color={colorMap.get(SubmissionType.SecondBlood)}
        />,
      ],
      [
        SubmissionType.ThirdBlood,
        <Icon
          path={mdiHexagonSlice2}
          size={size}
          color={colorMap.get(SubmissionType.ThirdBlood)}
        />,
      ],
    ]),
    colorMap,
  }
}

export const NoticTypeIconMap = (size: number) => {
  const theme = useMantineTheme()
  const { iconMap } = SubmissionTypeIconMap(size)
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
    [NoticeType.FirstBlood, iconMap.get(SubmissionType.FirstBlood)],
    [NoticeType.SecondBlood, iconMap.get(SubmissionType.SecondBlood)],
    [NoticeType.ThirdBlood, iconMap.get(SubmissionType.ThirdBlood)],
  ])
}

export const useParticipationStatusMap = () => {
  const { t } = useTranslation()

  return new Map([
    [
      ParticipationStatus.Pending,
      {
        title: t('game.participation.status.pending'),
        color: 'yellow',
        iconPath: mdiHelpCircleOutline,
        transformTo: [ParticipationStatus.Accepted, ParticipationStatus.Rejected],
      },
    ],
    [
      ParticipationStatus.Accepted,
      {
        title: t('game.participation.status.accepted'),
        color: 'green',
        iconPath: mdiCheck,
        transformTo: [ParticipationStatus.Suspended],
      },
    ],
    [
      ParticipationStatus.Rejected,
      {
        title: t('game.participation.status.rejected'),
        color: 'red',
        iconPath: mdiClose,
        transformTo: [],
      },
    ],
    [
      ParticipationStatus.Suspended,
      {
        title: t('game.participation.status.suspended'),
        color: 'alert',
        iconPath: mdiCancel,
        transformTo: [ParticipationStatus.Accepted],
      },
    ],
  ])
}

export interface BonusLabel {
  name: string
  descr: string
}

export class BloodBonus {
  static default = new BloodBonus()
  static base = 1000
  static mask = 0x3ff
  private val: number = (50 << 20) + (30 << 10) + 10

  constructor(val?: number) {
    this.val = val ?? this.val
  }

  get value() {
    return this.val
  }

  static fromBonus(first: number, second: number, third: number) {
    const value = (first << 20) + (second << 10) + third
    return new BloodBonus(value)
  }

  getBonusNum(type: SubmissionType) {
    if (type === SubmissionType.FirstBlood) return (this.val >> 20) & BloodBonus.mask
    if (type === SubmissionType.SecondBlood) return (this.val >> 10) & BloodBonus.mask
    if (type === SubmissionType.ThirdBlood) return this.val & BloodBonus.mask
    return 0
  }

  getBonus(type: SubmissionType) {
    if (type === SubmissionType.Unaccepted) return 0
    if (type === SubmissionType.Normal) return 1

    const num = this.getBonusNum(type)
    if (num === 0) return 0

    return num / BloodBonus.base
  }
}

export const useBonusLabels = (bonus: BloodBonus) => {
  const { t } = useTranslation()

  const BonusLabelNameMap = new Map([
    [SubmissionType.FirstBlood, t('challenge.bonus.first_blood')],
    [SubmissionType.SecondBlood, t('challenge.bonus.second_blood')],
    [SubmissionType.ThirdBlood, t('challenge.bonus.third_blood')],
  ])

  return new Map(
    BloodsTypes.map((type) => {
      const bonus_value = bonus.getBonusNum(type)
      return [
        type,
        {
          name: BonusLabelNameMap.get(type),
          descr: `+${bonus_value / (BloodBonus.base / 100)}%`,
        } as BonusLabel,
      ]
    })
  )
}

export const TaskStatusColorMap = new Map<TaskStatus | null, string>([
  [TaskStatus.Success, 'green'],
  [TaskStatus.Failed, 'red'],
  [TaskStatus.Pending, 'yellow'],
  [TaskStatus.Denied, 'alert'],
  [TaskStatus.Exit, 'gray'],
  [TaskStatus.NotFound, 'violet'],
  [TaskStatus.Duplicate, 'lime'],
  [null, 'gray'],
])

export const getProxyUrl = (guid: string, test: boolean = false) => {
  const protocol = window.location.protocol.replace('http', 'ws')
  const api = test ? 'api/proxy/noinst' : 'api/proxy'
  return `${protocol}//${window.location.host}/${api}/${guid}`
}

export const HunamizeSize = (size: number) => {
  if (size < 1024) {
    return `${size} B`
  } else if (size < 1024 * 1024) {
    return `${(size / 1024).toFixed(2)} KiB`
  } else if (size < 1024 * 1024 * 1024) {
    return `${(size / 1024 / 1024).toFixed(2)} MiB`
  } else {
    return `${(size / 1024 / 1024 / 1024).toFixed(2)} GiB`
  }
}
