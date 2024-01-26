import {
  Button,
  Center,
  Group,
  ScrollArea,
  Select,
  SimpleGrid,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiHexagonSlice6, mdiKeyboardBackspace, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { Dispatch, FC, SetStateAction, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import BloodBonusModel from '@Components/admin/BloodBonusModel'
import ChallengeCreateModal from '@Components/admin/ChallengeCreateModal'
import ChallengeEditCard from '@Components/admin/ChallengeEditCard'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ChallengeTagItem, useChallengeTagLabelMap } from '@Utils/Shared'
import { OnceSWRConfig } from '@Utils/useConfig'
import api, { ChallengeInfoModel, ChallengeTag } from '@Api'

const GameChallengeEdit: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const navigate = useNavigate()
  const [createOpened, setCreateOpened] = useState(false)
  const [bonusOpened, setBonusOpened] = useState(false)
  const [category, setCategory] = useState<ChallengeTag | null>(null)
  const challengeTagLabelMap = useChallengeTagLabelMap()

  const { t } = useTranslation()

  const { data: challenges, mutate } = api.edit.useEditGetGameChallenges(numId, OnceSWRConfig)

  const filteredChallenges =
    category && challenges ? challenges?.filter((c) => c.tag === category) : challenges
  filteredChallenges?.sort((a, b) => ((a.tag ?? '') > (b.tag ?? '') ? -1 : 1))

  const modals = useModals()
  const onToggle = (
    challenge: ChallengeInfoModel,
    setDisabled: Dispatch<SetStateAction<boolean>>
  ) => {
    modals.openConfirmModal({
      title: challenge.isEnabled
        ? t('admin.button.challenges.disable')
        : t('admin.button.challenges.enable'),
      children: (
        <Text size="sm">
          {challenge.isEnabled
            ? t('admin.content.games.challenges.disable')
            : t('admin.content.games.challenges.enable')}
        </Text>
      ),
      onConfirm: () => onConfirmToggle(challenge, setDisabled),
      confirmProps: { color: 'orange' },
    })
  }

  const onConfirmToggle = (
    challenge: ChallengeInfoModel,
    setDisabled: Dispatch<SetStateAction<boolean>>
  ) => {
    const numId = parseInt(id ?? '-1')
    setDisabled(true)
    api.edit
      .editUpdateGameChallenge(numId, challenge.id!, {
        isEnabled: !challenge.isEnabled,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutate(
          challenges?.map((c) =>
            c.id === challenge.id ? { ...c, isEnabled: !challenge.isEnabled } : c
          )
        )
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <WithGameEditTab
      headProps={{ position: 'apart' }}
      isLoading={!challenges}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            {t('admin.button.back')}
          </Button>
          <Group w="calc(100% - 9rem)" position="apart">
            <Select
              placeholder={t('admin.content.show_all')}
              clearable
              searchable
              nothingFound={t('admin.content.nothing_found')}
              value={category}
              onChange={(value: ChallengeTag) => setCategory(value)}
              itemComponent={ChallengeTagItem}
              data={Object.entries(ChallengeTag).map((tag) => {
                const data = challengeTagLabelMap.get(tag[1])
                return { value: tag[1], ...data }
              })}
            />
            <Group position="right">
              <Button
                leftIcon={<Icon path={mdiHexagonSlice6} size={1} />}
                onClick={() => setBonusOpened(true)}
              >
                {t('admin.button.challenges.bonus')}
              </Button>
              <Button
                mr="18px"
                leftIcon={<Icon path={mdiPlus} size={1} />}
                onClick={() => setCreateOpened(true)}
              >
                {t('admin.button.challenges.new')}
              </Button>
            </Group>
          </Group>
        </>
      }
    >
      <ScrollArea h="calc(100vh - 180px)" pos="relative" offsetScrollbars type="auto">
        {!filteredChallenges || filteredChallenges.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack spacing={0}>
              <Title order={2}>{t('admin.content.games.challenges.empty.title')}</Title>
              <Text>{t('admin.content.games.challenges.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <SimpleGrid
            cols={2}
            pr={6}
            breakpoints={[
              { maxWidth: 3600, cols: 2, spacing: 'sm' },
              { maxWidth: 1800, cols: 1, spacing: 'sm' },
            ]}
          >
            {filteredChallenges &&
              filteredChallenges.map((challenge) => (
                <ChallengeEditCard key={challenge.id} challenge={challenge} onToggle={onToggle} />
              ))}
          </SimpleGrid>
        )}
      </ScrollArea>
      <ChallengeCreateModal
        title={t('admin.button.challenges.new')}
        size="30%"
        opened={createOpened}
        onClose={() => setCreateOpened(false)}
        onAddChallenge={(challenge) => mutate([challenge, ...(challenges ?? [])])}
      />
      <BloodBonusModel
        title={t('admin.button.challenges.bonus')}
        size="30%"
        opened={bonusOpened}
        onClose={() => setBonusOpened(false)}
      />
    </WithGameEditTab>
  )
}

export default GameChallengeEdit
