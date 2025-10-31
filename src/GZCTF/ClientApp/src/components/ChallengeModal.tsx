import {
  Button,
  Divider,
  Group,
  Modal,
  ModalProps,
  Stack,
  TextInput,
  Text,
  Title,
  useMantineTheme,
  ScrollAreaAutosize,
  Input,
} from '@mantine/core'
import { mdiLightbulbOnOutline, mdiOpenInNew, mdiPackageVariantClosed } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { InstanceEntry } from '@Components/InstanceEntry'
import { ContentPlaceholder, InlineMarkdown, Markdown } from '@Components/MarkdownRenderer'
import { useLanguage } from '@Utils/I18n'
import { ChallengeCategoryItemProps } from '@Utils/Shared'
import { ChallengeDetailModel, ChallengeType } from '@Api'
import classes from '@Styles/ChallengeModal.module.css'
import misc from '@Styles/Misc.module.css'

dayjs.extend(duration)

interface ChallengeDeadlineNoticeProps {
  deadline: dayjs.Dayjs
  onExpiredChange: (expired: boolean) => void
}

const ChallengeDeadlineNotice: FC<ChallengeDeadlineNoticeProps> = ({ deadline, onExpiredChange }) => {
  const { t } = useTranslation()
  const [now, setNow] = useState(dayjs())
  const { locale } = useLanguage()

  useEffect(() => {
    setNow(dayjs())
    const timer = setInterval(() => setNow(dayjs()), 1000)
    return () => clearInterval(timer)
  }, [deadline])

  useEffect(() => {
    onExpiredChange(now.isAfter(deadline))
  }, [now, deadline, onExpiredChange])

  if (now.isAfter(deadline)) {
    return null
  }

  const formattedDeadline = useMemo(() => deadline.locale(locale).format('L LTS'), [deadline, locale])

  const diff = deadline.diff(now)
  const duration = dayjs.duration(diff)
  const countdownText = `${Math.floor(duration.asHours())}:${duration.format('mm:ss')}`

  return (
    <Group gap="xs" justify="space-between" wrap="nowrap">
      <Text fw="bold" size="sm">
        {t('challenge.content.deadline.remaining')}&nbsp;
        <Text span ff="monospace" fw="bold" size="sm" c="brand">
          {countdownText}
        </Text>
      </Text>
      <Text fw="bold" size="xs" c="dimmed">
        {t('challenge.content.deadline.label')}&nbsp;
        <Text span ff="monospace" c="dimmed" fw="bold" size="xs">
          {formattedDeadline}
        </Text>
      </Text>
    </Group>
  )
}

export interface ChallengeModalProps extends ModalProps {
  challenge?: ChallengeDetailModel
  cateData: ChallengeCategoryItemProps
  solved?: boolean
  disabled?: boolean
  gameTitle?: string
  gameEnded?: boolean
  practiceMode?: boolean
  flag: string
  setFlag: (value: string | React.ChangeEvent<any> | null | undefined) => void
  onCreate: () => void
  onExtend: () => void
  onDestroy: () => void
  onSubmitFlag: () => void
  onDownload?: () => void
}

export const ChallengeModal: FC<ChallengeModalProps> = (props) => {
  const {
    challenge,
    cateData,
    solved,
    disabled,
    gameTitle,
    gameEnded,
    practiceMode,
    flag,
    setFlag,
    onCreate,
    onExtend,
    onDestroy,
    onDownload,
    onSubmitFlag,
    ...modalProps
  } = props
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const { locale } = useLanguage()

  const placeholders = t('challenge.content.flag_placeholders', {
    returnObjects: true,
  }) as string[]

  const [placeholder, setPlaceholder] = useState('')
  useEffect(() => {
    setPlaceholder(placeholders[Math.floor(Math.random() * placeholders.length)])
  }, [challenge])

  const deadlineTime = useMemo(() => (challenge?.deadline ? dayjs(challenge.deadline) : null), [challenge?.deadline])
  const [isDeadlinePassed, setIsDeadlinePassed] = useState(() => (deadlineTime ? dayjs().isAfter(deadlineTime) : false))

  useEffect(() => {
    setIsDeadlinePassed(deadlineTime ? dayjs().isAfter(deadlineTime) : false)
  }, [deadlineTime])

  const isLimitReached = (challenge?.limit && (challenge.attempts ?? 0) >= challenge.limit) || false

  const isContainer =
    challenge?.type === ChallengeType.StaticContainer || challenge?.type === ChallengeType.DynamicContainer

  const title = (
    <Stack gap="xs">
      <Group wrap="nowrap" w="100%" justify="space-between" gap="sm">
        <Group wrap="nowrap" gap="sm" w="calc(100% - 6.75rem)">
          {cateData && <Icon path={cateData.icon} size={1.2} color={theme.colors[cateData.color][5]} />}
          <Title order={4} lineClamp={1}>
            {challenge?.title ?? ''}
          </Title>
        </Group>
        <Text miw="6rem" fw="bold" ff="monospace" ta="right">
          {challenge?.score ?? 0} pts
        </Text>
      </Group>
      <Divider size="md" color={cateData?.color} />
    </Stack>
  )

  const content = (
    <ScrollAreaAutosize mah="52vh" maw="100%" scrollbars="y" scrollbarSize={6} type="scroll">
      {challenge?.content === undefined ? (
        <ContentPlaceholder />
      ) : (
        <>
          <Markdown source={challenge.content ?? ''} />
          {challenge.hints && challenge.hints.length > 0 && (
            <Stack gap={2} pt="sm">
              {challenge.hints.map((hint) => (
                <Group key={hint} gap="xs" align="flex-start" wrap="nowrap">
                  <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]} />
                  <InlineMarkdown key={hint} size="sm" maw="calc(100% - 2rem)" source={hint} />
                </Group>
              ))}
            </Stack>
          )}
        </>
      )}
    </ScrollAreaAutosize>
  )

  const withDeadline = deadlineTime && !isDeadlinePassed
  const deadline = withDeadline && (
    <ChallengeDeadlineNotice deadline={deadlineTime} onExpiredChange={setIsDeadlinePassed} />
  )

  const withAttachment = !!challenge?.context?.url || onDownload

  const link = challenge?.context?.url
  const local = link && link.startsWith('/assets')

  const attachment = withAttachment && (
    <Group gap="xs" justify="flex-start" align="center" wrap="nowrap">
      <Text fw="bold" size="sm">
        {t('challenge.button.download.attachment')}
      </Text>
      <Button
        component="a"
        href={link ?? '#'}
        variant="light"
        size="compact-sm"
        target="_blank"
        rel="noreferrer"
        leftSection={<Icon path={local ? mdiPackageVariantClosed : mdiOpenInNew} size={0.8} />}
        maw="20rem"
        onClick={
          onDownload &&
          ((e: any) => {
            e.preventDefault()
            onDownload()
          })
        }
      >
        {local ? link.split('/').pop() : t('common.content.external_link')}
      </Button>
    </Group>
  )

  const withInstance = isContainer && challenge?.context

  const instance = withInstance && (
    <InstanceEntry
      label={`${challenge.title} @ ${gameTitle}`}
      context={challenge.context!}
      onCreate={onCreate}
      onExtend={onExtend}
      onDestroy={onDestroy}
      disabled={disabled}
    />
  )

  const attemptsInfo = useMemo(() => {
    if (typeof challenge?.attempts !== 'number' || solved) return null

    let content = null
    if (deadlineTime && isDeadlinePassed) {
      content = t('challenge.content.deadline.expired', {
        deadline: deadlineTime.locale(locale).format('L LTS'),
      })
    } else if (challenge?.limit) {
      const remaining = challenge.limit - challenge.attempts
      if (remaining > 0) {
        content = t('challenge.content.attempts.remaining', { remaining })
      } else {
        content = t('challenge.content.attempts.exhausted')
      }
    } else {
      content = t('challenge.content.attempts.count', { count: challenge.attempts })
    }

    return <Input.Label>{content}</Input.Label>
  }, [challenge?.attempts, challenge?.limit, solved, deadlineTime, locale, isDeadlinePassed, t])

  const inputValue = solved
    ? t('challenge.content.already_solved')
    : isLimitReached
      ? t('challenge.content.attempts.placeholder')
      : flag

  // Allow submission if deadline not passed OR (game ended AND practice mode enabled)
  const canSubmitDespiteDeadline = !isDeadlinePassed || (gameEnded && practiceMode)
  const inputDisabled = disabled || solved || isLimitReached || !canSubmitDespiteDeadline

  const footer = (
    <Stack gap="xs" className={classes.footer}>
      {(withAttachment || withInstance || withDeadline) && (
        <>
          <Divider mb={attemptsInfo ? '0.2rem' : undefined} />
          {attachment}
          {instance}
          {deadline}
        </>
      )}
      <Divider label={attemptsInfo} my={attemptsInfo ? '-0.4rem' : undefined} />
      <form
        onSubmit={(e) => {
          e.preventDefault()
          if (!solved && canSubmitDespiteDeadline) {
            onSubmitFlag()
          }
        }}
      >
        <Group justify="space-between" gap="sm" align="flex-end">
          <TextInput
            placeholder={placeholder}
            value={inputValue}
            disabled={inputDisabled}
            onChange={setFlag}
            classNames={{ root: misc.flexGrow, input: misc.ffmono }}
          />
          <Button miw="6rem" type="submit" disabled={inputDisabled}>
            {t('challenge.button.submit_flag')}
          </Button>
        </Group>
      </form>
    </Stack>
  )

  return (
    <Modal.Root
      size="42vw"
      {...modalProps}
      onClose={() => {
        setFlag('')
        modalProps.onClose()
      }}
      centered
      classNames={classes}
    >
      <Modal.Overlay />
      <Modal.Content>
        <Modal.Header>
          <Modal.Title>{title}</Modal.Title>
        </Modal.Header>
        <Modal.Body>{content}</Modal.Body>
        {footer}
      </Modal.Content>
    </Modal.Root>
  )
}
