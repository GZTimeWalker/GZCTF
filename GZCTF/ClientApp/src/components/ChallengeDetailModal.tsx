import { marked } from 'marked'
import { FC, useState } from 'react'
import {
  Button,
  Divider,
  Group,
  Modal,
  ModalProps,
  Stack,
  Text,
  Title,
  TypographyStylesProvider,
} from '@mantine/core'
import { Icon } from '@mdi/react'
import api from '../Api'
import { useTypographyStyles } from '../utils/ThemeOverride'
import { ChallengeTagItemProps } from './ChallengeItem'
import { mdiLightbulbOnOutline } from '@mdi/js'

interface ChallengeDetailModalProps extends ModalProps {
  gameId: number
  tagData: ChallengeTagItemProps
  title: string
  score: number
  challengeId: number | null
}

const ChallengeDetailModal: FC<ChallengeDetailModalProps> = (props) => {
  const { gameId, challengeId, tagData, title, score, ...modalProps } = props

  const [disabled] = useState(false)

  const { data: challenge } = api.game.useGameGetChallenge(gameId, challengeId ?? 0, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  const { classes, theme } = useTypographyStyles()

  return (
    <Modal
      {...modalProps}
      styles={{
        ...modalProps.styles,
        header: {
          margin: 0,
        },
        title: {
          width: '100%',
          margin: 0,
        },
      }}
      title={
        <Group style={{ width: '100%' }} position="apart">
          <Title order={4}>{challenge?.title ?? title}</Title>
          <Text weight={700} sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}>
            {challenge?.score ?? score} pts
          </Text>
        </Group>
      }
    >
      <Stack>
        <Divider
          size="sm"
          variant="dashed"
          color={tagData?.color}
          labelPosition="center"
          label={tagData && <Icon path={tagData.icon} size={1} />}
        />
        <TypographyStylesProvider className={classes.root} style={{ minHeight: '4rem' }}>
          <div dangerouslySetInnerHTML={{ __html: marked(challenge?.content ?? '') }} />
        </TypographyStylesProvider>
        {challenge && challenge.hints && (
          <Stack spacing={2}>
            {challenge.hints.split(';').map((hint) => (
              <Group spacing="xs" align="flex-start" noWrap>
                <Icon path={mdiLightbulbOnOutline} size={0.8} color={theme.colors.yellow[5]}/>
                <Text key={hint} size="sm" style={{ maxWidth: 'calc(100% - 2rem)' }}>{hint}</Text>
              </Group>
            ))}
          </Stack>
        )}
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth disabled={disabled}></Button>
          <Button fullWidth disabled={disabled}></Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default ChallengeDetailModal
