import {
  ActionIcon,
  Badge,
  Card,
  Group,
  Loader,
  Progress,
  Stack,
  Switch,
  Text,
  Tooltip,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDatabaseEditOutline, mdiHammerWrench, mdiPuzzleEditOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { Dispatch, FC, SetStateAction, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { useChallengeCategoryLabelMap, showErrorMsg } from '@Utils/Shared'
import api, { ChallengeInfoModel, ChallengeCategory } from '@Api'
import classes from '@Styles/ChallengeEditCard.module.css'

interface ChallengeEditCardProps {
  challenge: ChallengeInfoModel
  onToggle: (challenge: ChallengeInfoModel, setDisabled: Dispatch<SetStateAction<boolean>>) => void
}

export const ChallengeEditCard: FC<ChallengeEditCardProps> = ({ challenge, onToggle }) => {
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const data = challengeCategoryLabelMap.get(challenge.category as ChallengeCategory)
  const theme = useMantineTheme()
  const { id } = useParams()

  const [disabled, setDisabled] = useState(false)
  const [building, setBuilding] = useState(false)

  const { t } = useTranslation()
  const numId = parseInt(id ?? '-1')

  const inFlightBuild = challenge.buildStatus === 'Queued' || challenge.buildStatus === 'Building'

  // Only Container-type challenges can have a local Dockerfile to
  // build. Static/Dynamic Attachment challenges are file-only; showing
  // a Build button there is just noise. Same for challenges that
  // explicitly ship a registry image (NotApplicable).
  const isBuildable =
    (challenge.type === 'StaticContainer' || challenge.type === 'DynamicContainer')
    && challenge.buildStatus !== 'NotApplicable'

  const onBuildNow = async () => {
    if (challenge.id == null) return
    setBuilding(true)
    try {
      await api.edit.editRebuildChallengeImage(numId, challenge.id)
      showNotification({
        color: 'teal',
        message: t('admin.notification.builds.enqueued'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBuilding(false)
    }
  }
  const { colorScheme } = useMantineColorScheme()

  const color = data?.color ?? theme.primaryColor
  const colors = theme.colors[color]

  const minIdx = colorScheme === 'dark' ? 8 : 6
  const curIdx = colorScheme === 'dark' ? 6 : 4

  const [min, cur, tot] = [challenge.minScore ?? 0, challenge.score ?? 500, challenge.originalScore ?? 500]
  const minRate = (min / tot) * 100
  const curRate = (cur / tot) * 100

  const contentWidth = 'calc(100% - 12rem)'

  return (
    <Card shadow="sm" p="sm">
      <Group wrap="nowrap" justify="space-between" gap="xs">
        <Switch
          color={color}
          disabled={disabled}
          checked={challenge.isEnabled}
          onChange={() => onToggle(challenge, setDisabled)}
        />

        <Icon path={data!.icon} color={theme.colors[data?.color ?? theme.primaryColor][5]} size={1.2} />

        <Stack gap={0} maw={contentWidth} miw={contentWidth}>
          <Group gap={6} wrap="nowrap">
            <Text truncate fw="bold">
              {challenge.title}
            </Text>
            {challenge.reviewStatus === 'Pending' && (
              <Badge size="xs" color="yellow" variant="filled">
                {t('admin.content.review.badge.pending')}
              </Badge>
            )}
            {challenge.reviewStatus === 'Rejected' && (
              <Badge size="xs" color="red" variant="filled">
                {t('admin.content.review.badge.rejected')}
              </Badge>
            )}
            {challenge.buildStatus === 'Queued' && (
              <Badge size="xs" color="blue" variant="light">
                {t('admin.content.review.badge.queued')}
              </Badge>
            )}
            {challenge.buildStatus === 'Building' && (
              <Badge size="xs" color="yellow" variant="light">
                {t('admin.content.review.badge.building')}
              </Badge>
            )}
            {challenge.buildStatus === 'Success' && (
              <Badge size="xs" color="teal" variant="light">
                {t('admin.content.review.badge.built')}
              </Badge>
            )}
            {challenge.buildStatus === 'NotApplicable' && (
              <Tooltip label={t('admin.content.review.badge.not_applicable_help')} multiline w={240}>
                <Badge size="xs" color="gray" variant="light">
                  {t('admin.content.review.badge.not_applicable')}
                </Badge>
              </Tooltip>
            )}
            {challenge.buildStatus === 'MissingDockerfile' && (
              <Tooltip label={t('admin.content.review.badge.missing_dockerfile_help')} multiline w={260}>
                <Badge size="xs" color="orange" variant="light">
                  {t('admin.content.review.badge.missing_dockerfile')}
                </Badge>
              </Tooltip>
            )}
            {challenge.buildStatus === 'Failed' && (
              <Tooltip label={t('admin.content.review.badge.build_failed_help')} multiline w={240}>
                <Badge size="xs" color="red" variant="filled">
                  {t('admin.content.review.badge.build_failed')}
                </Badge>
              </Tooltip>
            )}
          </Group>
          <Text size="sm" fw="bold" ff="monospace" w="5rem">
            {challenge.score}
            <Text span fw="bold" c="dimmed">
              /{challenge.originalScore}pts
            </Text>
          </Text>
        </Stack>

        {isBuildable && (
          <Tooltip
            label={
              inFlightBuild
                ? t('admin.button.challenges.build_in_flight')
                : t('admin.button.challenges.build_now')
            }
            ta="end"
            position="left"
            offset={98}
            classNames={classes}
          >
            <ActionIcon
              c={color}
              variant="subtle"
              disabled={building || inFlightBuild}
              onClick={onBuildNow}
            >
              {building || inFlightBuild ? (
                <Loader size="xs" />
              ) : (
                <Icon path={mdiHammerWrench} size={1} />
              )}
            </ActionIcon>
          </Tooltip>
        )}
        <Tooltip label={t('admin.button.challenges.edit')} position="left" offset={10} classNames={classes}>
          <ActionIcon c={color} component={Link} to={`/admin/games/${id}/challenges/${challenge.id}`}>
            <Icon path={mdiPuzzleEditOutline} size={1} />
          </ActionIcon>
        </Tooltip>
        <Tooltip
          label={t('admin.button.challenges.edit_more')}
          ta="end"
          position="left"
          offset={54}
          classNames={classes}
        >
          <ActionIcon c={color} component={Link} to={`/admin/games/${id}/challenges/${challenge.id}/flags`}>
            <Icon path={mdiDatabaseEditOutline} size={1} />
          </ActionIcon>
        </Tooltip>
      </Group>

      <Card.Section mt="sm">
        <Progress.Root radius={0}>
          <Progress.Section value={minRate} color={colors[minIdx]} />
          <Progress.Section value={curRate - minRate} color={colors[curIdx]} />
        </Progress.Root>
      </Card.Section>
    </Card>
  )
}
