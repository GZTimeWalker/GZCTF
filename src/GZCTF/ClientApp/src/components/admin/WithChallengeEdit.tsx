import { Stack, Group, Button, Text, useMantineTheme } from '@mantine/core'
import { mdiArrowLeft, mdiArrowRight } from '@mdi/js'
import Icon from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams, useLocation } from 'react-router'
import { WithGameEditTab, GameEditTabProps } from '@Components/admin/WithGameEditTab'
import { useChallengeCategoryLabelMap } from '@Utils/Shared'
import { useEditChallenges } from '@Hooks/useEdit'
import { ChallengeInfoModel, ChallengeCategory } from '@Api'

export const WithChallengeEdit: FC<GameEditTabProps> = (props) => {
  const { children, isLoading, ...rest } = props
  const location = useLocation()
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const { challenges } = useEditChallenges(numId)
  const { t } = useTranslation()
  const theme = useMantineTheme()

  const getBeforeNext = (challenges: ChallengeInfoModel[], id: number) => {
    const index = challenges.findIndex((chal) => chal.id === id)
    return {
      prev: challenges[index - 1],
      current: challenges[index],
      next: challenges[index + 1],
    }
  }

  const { prev, current, next } = challenges
    ? getBeforeNext(challenges, numCId)
    : { prev: null, current: null, next: null }
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()

  const color = (chal: ChallengeInfoModel | null) => {
    const c = !chal
      ? theme.primaryColor
      : (challengeCategoryLabelMap.get(chal.category as ChallengeCategory)?.color ?? theme.primaryColor)

    return c
  }

  const regex = /\/admin\/games\/\d+\/challenges\/\d+/
  const restpath = location.pathname.replace(regex, '')

  return (
    <WithGameEditTab isLoading={isLoading} {...rest}>
      <Stack mih="calc(100vh - 12rem)" justify="space-between">
        {children}
        <Group justify="space-between" w="100%" wrap="nowrap">
          <Button
            justify="space-between"
            component={Link}
            disabled={isLoading || !prev}
            leftSection={<Icon path={mdiArrowLeft} size={1} />}
            to={prev?.id ? `/admin/games/${numId}/challenges/${prev?.id}${restpath}` : '#'}
          >
            {t('admin.button.challenges.previous')}
          </Button>

          <Group justify="space-between" gap="xs" wrap="nowrap" maw="calc(100% - 16rem)">
            <Text c="dimmed" truncate>
              {prev?.title ?? ''}
            </Text>
            <Text fw="bold" c={color(current)} truncate>
              {current?.title ?? ''}
            </Text>
            <Text c="dimmed" truncate>
              {next?.title ?? ''}
            </Text>
          </Group>

          <Button
            disabled={isLoading || !next}
            justify="space-between"
            component={Link}
            rightSection={<Icon path={mdiArrowRight} size={1} />}
            to={next?.id ? `/admin/games/${numId}/challenges/${next?.id}${restpath}` : '#'}
          >
            {t('admin.button.challenges.next')}
          </Button>
        </Group>
      </Stack>
    </WithGameEditTab>
  )
}
