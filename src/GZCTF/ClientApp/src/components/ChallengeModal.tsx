import {
  Alert,
  Avatar,
  Button,
  Divider,
  Group,
  Modal,
  ModalProps,
  ScrollArea,
  Stack,
  TextInput,
  Text,
  Title,
  useMantineTheme,
  ScrollAreaAutosize,
  Input,
  Textarea,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiAlertCircleOutline, mdiFlag, mdiHexagonSlice2, mdiHexagonSlice4, mdiHexagonSlice6, mdiLightbulbOnOutline, mdiOpenInNew, mdiPackageVariantClosed, mdiThumbUp, mdiThumbDown } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import duration from 'dayjs/plugin/duration'
import relativeTime from 'dayjs/plugin/relativeTime'

dayjs.extend(relativeTime)
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { InstanceEntry } from '@Components/InstanceEntry'
import { ContentPlaceholder, InlineMarkdown, Markdown } from '@Components/MarkdownRenderer'
import { useLanguage } from '@Utils/I18n'
import { ChallengeCategoryItemProps } from '@Utils/Shared'
import { useTicker } from '@Hooks/useTicker'
import { ChallengeDetailModel, ChallengeType, ReviewRating, SubmissionType } from '@Api'

export interface SolverInfo {
  rank: number
  teamName: string
  teamAvatar: string | null
  userName: string | null
  type: SubmissionType
  time: number
  score: number
}
import classes from '@Styles/ChallengeModal.module.css'
import misc from '@Styles/Misc.module.css'

dayjs.extend(duration)

interface ChallengeDeadlineNoticeProps {
  deadline: dayjs.Dayjs
  onExpiredChange: (expired: boolean) => void
}

const ChallengeDeadlineNotice: FC<ChallengeDeadlineNoticeProps> = ({ deadline, onExpiredChange }) => {
  const { t } = useTranslation()
  // Shared 1s ticker so multiple deadline widgets share one interval.
  const now = useTicker()
  const { locale } = useLanguage()

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
  /** True while a flag submission is in-flight (network or server-side
   *  check) — renders a spinner inside the submit button. */
  submitting?: boolean
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
  onReviewSubmit?: (rating: ReviewRating, comment: string) => Promise<void>
  /** True only when the flag was accepted in this browser session (not a pre-existing solve). */
  justSolved?: boolean
  solvers?: SolverInfo[]
}

export const ChallengeModal: FC<ChallengeModalProps> = (props) => {
  const {
    challenge,
    cateData,
    solved,
    justSolved,
    disabled,
    submitting,
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
    onReviewSubmit,
    solvers,
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

  const [rating, setRating] = useState<ReviewRating>(ReviewRating.None)
  const [comment, setComment] = useState('')
  const [isSubmittingReview, setIsSubmittingReview] = useState(false)
  const [reviewSubmitted, setReviewSubmitted] = useState(false)

  // Reset review state only when a fresh in-session solve occurs
  useEffect(() => {
    if (justSolved) setReviewSubmitted(false)
  }, [justSolved])

  useEffect(() => {
    if (challenge) {
      setRating((challenge as any).userRating ?? ReviewRating.None)
      setComment((challenge as any).userComment ?? '')
    }
  }, [challenge])

  // Block close only for challenges solved in this session that haven't been reviewed yet
  const handleClose = () => {
    if (justSolved && !reviewSubmitted) {
      showNotification({
        color: 'orange',
        message: t('challenge.review.required_to_close', 'Please rate this challenge before closing'),
        icon: <Icon path={mdiAlertCircleOutline} size={1} />,
        autoClose: 3000,
      })
      return
    }
    setFlag('')
    modalProps.onClose()
  }

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

  const solverIconMap = new Map([
    [SubmissionType.FirstBlood,  { path: mdiHexagonSlice6, color: theme.colors.yellow[5] }],
    [SubmissionType.SecondBlood, { path: mdiHexagonSlice4, color: theme.colors.gray[4] }],
    [SubmissionType.ThirdBlood,  { path: mdiHexagonSlice2, color: theme.colors.orange[6] }],
    [SubmissionType.Normal,      { path: mdiFlag,          color: theme.colors[theme.primaryColor][5] }],
  ])

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

          {solvers && solvers.length > 0 && (
            <Stack gap={4} pt="md">
              <Divider
                label={
                  <Text size="xs" c="dimmed" fw={500}>
                    Solved by {solvers.length} {solvers.length === 1 ? 'team' : 'teams'}
                  </Text>
                }
                labelPosition="left"
              />
              <ScrollArea h={Math.min(solvers.length * 34, 170)} scrollbarSize={4}>
                <Stack gap={2}>
                  {solvers.map((s, i) => {
                    const icon = solverIconMap.get(s.type) ?? solverIconMap.get(SubmissionType.Normal)!
                    return (
                      <Group key={i} gap="xs" wrap="nowrap" px={2}>
                        <Icon path={icon.path} size={0.75} color={icon.color} />
                        <Avatar
                          src={s.teamAvatar}
                          size={20}
                          radius="xl"
                          style={{ flexShrink: 0 }}
                        >
                          {s.teamName.slice(0, 1)}
                        </Avatar>
                        <Text size="xs" fw={600} truncate maw="8rem" style={{ flexShrink: 0 }}>
                          {s.teamName}
                        </Text>
                        {s.userName && (
                          <Text size="xs" c="dimmed" truncate maw="7rem">
                            {s.userName}
                          </Text>
                        )}
                        <Text size="xs" c="dimmed" ff="monospace" ml="auto" style={{ flexShrink: 0 }}>
                          {dayjs(s.time).fromNow()}
                        </Text>
                      </Group>
                    )
                  })}
                </Stack>
              </ScrollArea>
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

  const reviewSection = justSolved && !reviewSubmitted && (
    <Stack gap="sm">
      <Divider label={t('challenge.review.label', 'Rate this challenge')} labelPosition="center" />
      <Alert icon={<Icon path={mdiAlertCircleOutline} size={0.9} />} color="orange" p="xs">
        <Text size="xs">{t('challenge.review.required_notice', 'Please rate this challenge — you can close after submitting.')}</Text>
      </Alert>
      <Group grow>
        <Button
          variant={rating === ReviewRating.Like ? 'filled' : 'default'}
          color="teal"
          radius="md"
          size="md"
          leftSection={<Icon path={mdiThumbUp} size="1.2rem" />}
          onClick={() => setRating(ReviewRating.Like)}
          styles={(theme) => ({
            root: {
              borderColor: rating === ReviewRating.Like ? undefined : theme.colors.teal[6],
              color: rating === ReviewRating.Like ? undefined : theme.colors.teal[6],
              borderWidth: rating === ReviewRating.Like ? undefined : '1px',
            },
          })}
        >
          {t('common.label.like', 'Recommended')}
        </Button>
        <Button
          variant={rating === ReviewRating.Dislike ? 'filled' : 'default'}
          color="red"
          radius="md"
          size="md"
          leftSection={<Icon path={mdiThumbDown} size="1.2rem" />}
          onClick={() => setRating(ReviewRating.Dislike)}
          styles={(theme) => ({
            root: {
              borderColor: rating === ReviewRating.Dislike ? undefined : theme.colors.red[6],
              color: rating === ReviewRating.Dislike ? undefined : theme.colors.red[6],
              borderWidth: rating === ReviewRating.Dislike ? undefined : '1px',
            },
          })}
        >
          {t('common.label.dislike', 'Not Recommended')}
        </Button>
      </Group>

      <Stack gap={4}>
        <Textarea
          placeholder={t('challenge.review.placeholder', 'Leave a comment...')}
          value={comment}
          autosize
          minRows={3}
          maxRows={6}
          maxLength={1000}
          onChange={(e) => setComment(e.currentTarget.value)}
        />
        <Group justify="space-between">
          <Text size="xs" c="dimmed">
            {/* Spacer or additional info if needed */}
          </Text>
          <Text size="xs" c={comment.length >= 1000 ? 'red' : 'dimmed'}>
            {comment.length} / 1000
          </Text>
        </Group>
      </Stack>

      <Group justify="flex-end">
        <Button
          loading={isSubmittingReview}
          disabled={rating === ReviewRating.None}
          onClick={async () => {
            if (onReviewSubmit) {
              setIsSubmittingReview(true)
              await onReviewSubmit(rating, comment)
              setIsSubmittingReview(false)
              setReviewSubmitted(true)
              setFlag('')
              modalProps.onClose()
            }
          }}
        >
          {t('challenge.review.submit_and_close', 'Submit & Close')}
        </Button>
      </Group>
    </Stack>
  )

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
          <Button
            miw="6rem"
            type="submit"
            disabled={inputDisabled}
            loading={submitting}
          >
            {t('challenge.button.submit_flag')}
          </Button>
        </Group>
      </form>
      {reviewSection}
    </Stack>
  )

  return (
    <Modal.Root
      size="42vw"
      {...modalProps}
      onClose={handleClose}
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
