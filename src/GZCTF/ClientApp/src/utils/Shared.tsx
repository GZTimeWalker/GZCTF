import {
  Group,
  Stack,
  Text,
  darken,
  lighten,
  useMantineTheme,
  useMantineColorScheme,
  SelectProps,
  ComboboxItem,
  MantineColorsTuple,
  OverlayProps,
} from '@mantine/core'
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
  mdiLanPending,
  mdiLightbulbOnOutline,
  mdiMatrix,
  mdiPlus,
  mdiRobotLoveOutline,
  mdiSearchWeb,
  mdiWeb,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { useTranslation } from 'react-i18next'
import { ChallengeCategory, ChallengeType, NoticeType, ParticipationStatus, SubmissionType, TaskStatus } from '@Api'

export const useChallengeTypeLabelMap = () => {
  const { t } = useTranslation()

  return new Map<ChallengeType, ChallengeTypeItemProps>([
    [
      ChallengeType.StaticAttachment,
      {
        name: t('challenge.type.static_attachment.label'),
        desrc: t('challenge.type.static_attachment.desrc'),
      },
    ],
    [
      ChallengeType.StaticContainer,
      {
        name: t('challenge.type.static_container.label'),
        desrc: t('challenge.type.static_container.desrc'),
      },
    ],
    [
      ChallengeType.DynamicAttachment,
      {
        name: t('challenge.type.dynamic_attachment.label'),
        desrc: t('challenge.type.dynamic_attachment.desrc'),
      },
    ],
    [
      ChallengeType.DynamicContainer,
      {
        name: t('challenge.type.dynamic_container.label'),
        desrc: t('challenge.type.dynamic_container.desrc'),
      },
    ],
  ])
}

export interface ChallengeTypeItemProps {
  name: string
  desrc: string
}

type SelectChallengeTypeItemProps = ChallengeTypeItemProps & ComboboxItem

export const ChallengeTypeItem: SelectProps['renderOption'] = ({ option }) => {
  const { name, desrc } = option as SelectChallengeTypeItemProps

  return (
    <Stack gap={0}>
      <Text size="sm" fw="bold">
        {name}
      </Text>
      <Text size="xs">{desrc}</Text>
    </Stack>
  )
}

export const ChallengeCategoryList = Object.values(ChallengeCategory)

export const useChallengeCategoryLabelMap = () => {
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const revert = colorScheme === 'dark' ? 'light' : 'dark'

  return new Map<ChallengeCategory, ChallengeCategoryItemProps>([
    [
      ChallengeCategory.Misc,
      {
        desrc: t('challenge.category.misc'),
        icon: mdiGamepadVariantOutline,
        name: ChallengeCategory.Misc,
        color: 'teal',
        colors: theme.colors['teal'],
      },
    ],
    [
      ChallengeCategory.Pwn,
      {
        desrc: t('challenge.category.pwn'),
        icon: mdiBomb,
        name: ChallengeCategory.Pwn,
        color: 'red',
        colors: theme.colors['red'],
      },
    ],
    [
      ChallengeCategory.Web,
      {
        desrc: t('challenge.category.web'),
        icon: mdiWeb,
        name: ChallengeCategory.Web,
        color: 'blue',
        colors: theme.colors['blue'],
      },
    ],
    [
      ChallengeCategory.Reverse,
      {
        desrc: t('challenge.category.reverse'),
        icon: mdiChevronTripleLeft,
        name: ChallengeCategory.Reverse,
        color: 'yellow',
        colors: theme.colors['yellow'],
      },
    ],
    [
      ChallengeCategory.Crypto,
      {
        desrc: t('challenge.category.crypto'),
        icon: mdiMatrix,
        name: ChallengeCategory.Crypto,
        color: 'violet',
        colors: theme.colors['violet'],
      },
    ],
    [
      ChallengeCategory.Blockchain,
      {
        desrc: t('challenge.category.blockchain'),
        icon: mdiEthereum,
        name: ChallengeCategory.Blockchain,
        color: 'green',
        colors: theme.colors['green'],
      },
    ],
    [
      ChallengeCategory.Forensics,
      {
        desrc: t('challenge.category.forensics'),
        icon: mdiFingerprint,
        name: ChallengeCategory.Forensics,
        color: 'indigo',
        colors: theme.colors['indigo'],
      },
    ],
    [
      ChallengeCategory.Hardware,
      {
        desrc: t('challenge.category.hardware'),
        icon: mdiChip,
        name: ChallengeCategory.Hardware,
        color: revert,
        colors: theme.colors[revert],
      },
    ],
    [
      ChallengeCategory.Mobile,
      {
        desrc: t('challenge.category.mobile'),
        icon: mdiCellphoneCog,
        name: ChallengeCategory.Mobile,
        color: 'pink',
        colors: theme.colors['pink'],
      },
    ],
    [
      ChallengeCategory.PPC,
      {
        desrc: t('challenge.category.ppc'),
        icon: mdiConsole,
        name: ChallengeCategory.PPC,
        color: 'cyan',
        colors: theme.colors['cyan'],
      },
    ],
    [
      ChallengeCategory.AI,
      {
        desrc: t('challenge.category.ai'),
        icon: mdiRobotLoveOutline,
        name: ChallengeCategory.AI,
        color: 'lime',
        colors: theme.colors['lime'],
      },
    ],
    [
      ChallengeCategory.OSINT,
      {
        desrc: t('challenge.category.osint'),
        icon: mdiSearchWeb,
        name: ChallengeCategory.OSINT,
        color: 'orange',
        colors: theme.colors['orange'],
      },
    ],
    [
      ChallengeCategory.Pentest,
      {
        desrc: t('challenge.category.pentest'),
        icon: mdiLanPending,
        name: ChallengeCategory.Pentest,
        color: 'grape',
        colors: theme.colors['grape'],
      },
    ],
  ])
}

export interface ChallengeCategoryItemProps {
  name: ChallengeCategory
  desrc: string
  icon: string
  color: string
  colors: MantineColorsTuple
}

type SelectChallengeCategoryItemProps = ChallengeCategoryItemProps & ComboboxItem

export const ChallengeCategoryItem: SelectProps['renderOption'] = ({ option }) => {
  const { colors, icon, name, desrc } = option as SelectChallengeCategoryItemProps

  return (
    <Group wrap="nowrap">
      <Icon color={colors[5]} path={icon} size={1.2} />
      <Stack gap={0}>
        <Text size="sm" fw="bold">
          {name}
        </Text>
        <Text size="xs">{desrc}</Text>
      </Stack>
    </Group>
  )
}

export const BloodsTypes = [SubmissionType.FirstBlood, SubmissionType.SecondBlood, SubmissionType.ThirdBlood]

export const SubmissionTypeColorMap = () => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  return new Map([
    [SubmissionType.Unaccepted, undefined],
    [SubmissionType.Normal, theme.colors[theme.primaryColor][colorScheme === 'dark' ? 8 : 6]],
    [SubmissionType.FirstBlood, theme.colors.yellow[5]],
    [
      SubmissionType.SecondBlood,
      colorScheme === 'dark' ? lighten(theme.colors.gray[2], 0.3) : darken(theme.colors.gray[1], 0.2),
    ],
    [
      SubmissionType.ThirdBlood,
      colorScheme === 'dark' ? darken(theme.colors.orange[7], 0.25) : lighten(theme.colors.orange[7], 0.2),
    ],
  ])
}

export const SubmissionTypeIconMap = (size: number) => {
  const colorMap = SubmissionTypeColorMap()
  return {
    iconMap: new Map<SubmissionType, PartialIconProps | undefined>([
      [SubmissionType.Unaccepted, undefined],
      [SubmissionType.Normal, { path: mdiFlag, size: size, color: colorMap.get(SubmissionType.Normal) }],
      [
        SubmissionType.FirstBlood,
        { path: mdiHexagonSlice6, size: size, color: colorMap.get(SubmissionType.FirstBlood) },
      ],
      [
        SubmissionType.SecondBlood,
        { path: mdiHexagonSlice4, size: size, color: colorMap.get(SubmissionType.SecondBlood) },
      ],
      [
        SubmissionType.ThirdBlood,
        { path: mdiHexagonSlice2, size: size, color: colorMap.get(SubmissionType.ThirdBlood) },
      ],
    ]),
    colorMap,
  }
}

export const NoticTypeIconMap = (size: number) => {
  const theme = useMantineTheme()
  const { iconMap } = SubmissionTypeIconMap(size)
  const { colorScheme } = useMantineColorScheme()
  const colorIdx = colorScheme === 'dark' ? 4 : 7

  return new Map([
    [NoticeType.Normal, { path: mdiBullhornOutline, size: size, color: theme.colors[theme.primaryColor][colorIdx] }],
    [NoticeType.NewHint, { path: mdiLightbulbOnOutline, size: size, color: theme.colors.yellow[colorIdx] }],
    [NoticeType.NewChallenge, { path: mdiPlus, size: size, color: theme.colors.green[colorIdx] }],
    [NoticeType.FirstBlood, iconMap.get(SubmissionType.FirstBlood)],
    [NoticeType.SecondBlood, iconMap.get(SubmissionType.SecondBlood)],
    [NoticeType.ThirdBlood, iconMap.get(SubmissionType.ThirdBlood)],
  ])
}

export interface PartialIconProps {
  path: string
  size: number
  color: string | undefined
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
        color: 'gray',
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
      const bonusValue = bonus.getBonusNum(type)
      return [
        type,
        {
          name: BonusLabelNameMap.get(type),
          descr: `+${bonusValue / (BloodBonus.base / 100)}%`,
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

export const DEFAULT_LOADING_OVERLAY: OverlayProps = {
  backgroundOpacity: 0.5,
  blur: 8,
}

export const IMAGE_MIME_TYPES = ['image/png', 'image/gif', 'image/jpeg', 'image/webp', 'image/avif', 'image/heic']

/** 系统错误信息 */
export const enum ErrorCodes {
  /**
   * 比赛未开始
   */
  GameNotStarted = 10001,

  /**
   * 比赛已结束
   */
  GameEnded = 10002,
}
